using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using Camille.Xmpp;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Camille;

public class XmppClient
{
    private readonly string _clientId = Guid.NewGuid().ToString();

    private readonly Thread _readThread;
    private readonly Thread _writeThread;

    private readonly Stream _stream;
    private readonly TcpClient _client;

    private readonly ILogger _logger;
    
    private bool _handshakeCompleted = false;
    private bool _isRunning = true;

    private BasicJid? _jid = null;
    private MySqlConnection _sqlConnection;
    
    private Func<XmppClient, bool> _disconnectCallback;
    
    private readonly List<XmppElement> _writeQueue = [];

    public XmppClient(TcpClient client, Stream stream)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = factory.CreateLogger(_clientId);
        _logger.LogInformation("Accepting client {}", _clientId);
        
        _stream = stream;
        _client = client;
        
        var builder = new MySqlConnectionStringBuilder()
        {
            Server = XmppServer.ConfigurationRoot["databaseSource"],
            UserID = XmppServer.ConfigurationRoot["databaseUsername"],
            Password = XmppServer.ConfigurationRoot["databasePassword"],
            Database= "nexus",
            Pooling = true,
        };
        var connectionString = builder.ConnectionString;

        _sqlConnection = new MySqlConnection(connectionString);
        _sqlConnection.Open();
        
        _writeThread = new Thread(WriteLoop);
        _readThread = new Thread(ReadLoop);
        
        _readThread.Start();
        _writeThread.Start();
    }

    public void Close(bool closeStream)
    {
        _isRunning = false;
        if (closeStream && (_stream.CanRead || _stream.CanWrite))
        {
            _stream.Close();
        }
        _sqlConnection.Close();
        if (_client.Connected)
        {
            _client.Close();
        }
    }
    
    public string GetClientId()
    {
        return _clientId;
    }

    private void OnXmlNode(XmlReader reader)
    {
        #if DEBUG 
                _logger.LogDebug("Stanza Type: {type}", reader.NodeType.ToString());
        #endif
        switch (reader.NodeType)
        {
            // Just ignore this for now
            case XmlNodeType.XmlDeclaration:
                break;
            case XmlNodeType.Element:
                OnXmlElement(reader);
                break;
            case XmlNodeType.EndElement:
                OnXmlEndElement(reader);
                break;
            default:
                _logger.LogError("Unknown node: {type}, {name}, {attributeCount}", reader.NodeType.ToString(), reader.Name, reader.AttributeCount); 
                break;
        }
    }

    public void SetDisconnectCallback(Func<XmppClient, bool> callback)
    {
        _disconnectCallback = callback;
    }

    public Stream GetStream()
    {
        return _stream;
    }

    public TcpClient GetTcpClient()
    {
        return _client;
    }

    private void OnXmlElement(XmlReader reader)
    {
        #if DEBUG 
            _logger.LogDebug("{type}, {value}, {namespace}, {name}", reader.NodeType.ToString(), reader.Value, reader.NamespaceURI, reader.Name);
        #endif
        try
        {
            switch (reader.Name)
            {
                case "stream:stream":
                    if (!Handshake())
                    {
                        _disconnectCallback(this);
                    }
                    return;
                case "auth":
                    OnAuthElement(reader);
                    break;
                case "iq":
                    OnIqStanza(reader);
                    break;
                default: 
                    _logger.LogDebug("Name: {name}", reader.Name);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical("{}", e.ToString());
        }
    }

    private void OnIqStanza(XmlReader reader)
    {
        var type = reader.GetAttribute("type");
        if (type == null)
        {
            throw new XmlException("IQ stanza did not have a type");
        }
        switch (type)
        {
            case "get":
                OnGetIqStanza(reader);
                break;
            default:
                throw new XmlException("Invalid IQ Stanza type");
        } 
    }

    private void OnGetIqStanza(XmlReader reader)
    {
        var id = reader.GetAttribute("id");
        if (id == null)
        {
            throw new XmlException("invalid iq stanza id");
        }
        // Now we need the query
        if (!reader.Read())
        {
            throw new XmlException("Could not read IQ contents");
        }

        switch (reader.NamespaceURI)
        {
            case "jabber:iq:register":
                var res = new RegisterResponseElement("stream",
                    "",
                    new XmlDocument());
                res.Id = id;
                _writeQueue.Add(res); 
                break;
            default:
                throw new XmlException("Invalid namespace uri");
        }
    }
    
    private void OnXmlEndElement(XmlReader reader)
    {
        #if DEBUG 
                _logger.LogDebug("{type}, {value}, {namespace}, {name}", reader.NodeType.ToString(), reader.Value, reader.NamespaceURI, reader.Name);
        #endif
        try
        {
            if (reader.Name == "stream:stream")
            {
                _disconnectCallback(this);
                return;
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical("{}", e.ToString());
        }
    }

    private void OnAuthElement(XmlReader element)
    {
        element.Read();
        if (AuthenticateUser(element.Value))
        {
            OnAuthSuccess();
        }
        else
        {
           OnAuthError(); 
        }
    }

    private bool AuthenticateUser(string stanzaValue)
    {
        if (stanzaValue.Length == 0)
        {
            return false;
        }
        _jid = GetAuthDetailsFromB64(stanzaValue);
        if (_jid == null)
        {
            return false;
        }
       
        var sql = "SELECT username, password FROM users WHERE username = {0}";
        var cmd = _sqlConnection.CreateCommand();
        cmd.CommandText = @"SELECT username, password FROM users WHERE username=@name AND password=@password";
        cmd.Parameters.Add(new MySqlParameter("name", MySqlDbType.VarChar) {Value = _jid.Username});
        cmd.Parameters.Add(new MySqlParameter("password", MySqlDbType.VarChar) {Value = _jid.Password});
        
        var reader = cmd.ExecuteReader();
        // No users found
        if (!reader.Read())
        {
            _logger.LogInformation("Could not authenticate as: {username}", _jid.Username);
            return false;
        }
        _logger.LogInformation("Authenticated as: {username}", reader.GetString(0));
        _jid.IsAuthenticated = true;
        
        return true;
    }

    private static BasicJid? GetAuthDetailsFromB64(string b64)
    {
        // The value of the Auth Element contains a base64 encoded string
        // The string consists of:
        // <username>@<domain>\0<username>\0AIR_<password>
        // With <username>@<domain> being the user's Jabber ID
        var base64Data = Convert.FromBase64String(b64);
        var base64String = Encoding.UTF8.GetString(base64Data);
        var splitted = base64String.Split("\0"); 
        
        // I'm not chancing anything weird
        if (splitted.Length != 3)
        {
            return null;
        }
        var jid = splitted[0];
        var username = splitted[1];
        var password = splitted[2].Replace("AIR_", "");

        return new BasicJid(jid, username, password);
    }

    private void OnAuthSuccess()
    {
        var response = new SuccessElement(null, null, new XmlDocument());
        _handshakeCompleted = true;
        _writeQueue.Add(response);
    }

    private void OnAuthError()
    {
        _disconnectCallback(this);
    }

    private bool Handshake()
    {
        StreamElement stream = new StreamElement("stream",
            "http://etherx.jabber.org/streams",
            new XmlDocument());
        _writeQueue.Add(stream);

        if (!_handshakeCompleted)
        {
            StartTlsElement tls = new StartTlsElement("stream",
                "http://etherx.jabber.org/streams",
                new XmlDocument());
            _writeQueue.Add(tls);
        }
        else
        {
            var features = new RegisterFeatures("stream", null, new XmlDocument());
            _writeQueue.Add(features);
        }
        _logger.LogInformation("Successful handshake response"); 
        return true;
    }
    private void ReadLoop()
    {
        var settings = new XmlReaderSettings
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            CloseInput = false
        };
        while (_isRunning)
        {
            try
            {
                using (var reader = XmlReader.Create(_stream, settings))
                {
                    while (_isRunning)
                    {
                        while (reader.Read())
                        {
                            OnXmlNode(reader);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical("{}", e.ToString());
                _disconnectCallback(this);
            }
        }
    }

    private void WriteLoop()
    {
        using var writer = new StreamWriter(_stream);
        try
        {
            while (_isRunning)
            {
                while (_writeQueue.Count > 0 && _isRunning)
                {
                    writer.Flush();
                    var ele = _writeQueue.First();
                    ele.Send(writer);
                    _writeQueue.RemoveAt(0);
                    writer.Flush();
                }

                Thread.Sleep(100);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical("{}", e.ToString());
        }
        finally
        {
            writer.Close();
            _disconnectCallback(this);
        }
    }
}