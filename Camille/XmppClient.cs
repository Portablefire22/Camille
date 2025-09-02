using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Xml;
using Camille.Json;
using Camille.Xmpp;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using StackExchange.Redis;

namespace Camille;

public class XmppClient
{
    private readonly string _clientId = Guid.NewGuid().ToString();

    private readonly Thread _readThread;
    private readonly Thread _writeThread;

    private ConnectionMultiplexer _redis;
    private IDatabase _db;
    
    private readonly Stream _stream;
    private readonly TcpClient _client;

    private readonly ILogger _logger;
    
    private bool _handshakeCompleted;
    private bool _isRunning = true;

    private BasicJid? _jid;
    private readonly MySqlConnection _sqlConnection;

    private Presence? _presence;
    
    private Func<XmppClient, bool> _disconnectCallback;
    
    private readonly List<XmppElement> _writeQueue = [];

    public XmppClient(TcpClient client, Stream stream)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = factory.CreateLogger(_clientId);
        _logger.LogInformation("Accepting client {}", _clientId);
        
        _stream = stream;
        _client = client;
        
        // Connect to main shared DB
        var builder = new MySqlConnectionStringBuilder()
        {
            Server = XmppServer.ConfigurationRoot["databaseSource"],
            UserID = XmppServer.ConfigurationRoot["databaseUsername"],
            Password = XmppServer.ConfigurationRoot["databasePassword"],
            Database = XmppServer.ConfigurationRoot["databaseName"],
            Pooling = true,
        };
        var connectionString = builder.ConnectionString;

        _sqlConnection = new MySqlConnection(connectionString);
        _sqlConnection.Open();
        
        // Connect to Camille only Redis server
        _redis = ConnectionMultiplexer.Connect("localhost");
        _db = _redis.GetDatabase();

        _writeThread = new Thread(WriteLoop);
        _readThread = new Thread(ReadLoop);
        
        _readThread.Start();
        _writeThread.Start();
    }

    private void SubscribeToChannel(string channel)
    {
        _redis.GetSubscriber().Subscribe(channel).OnMessage(OnRedisMessage);
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
                _logger.LogError("Unknown node: {type}, {name}, {attributeCount}, {value}", reader.NodeType.ToString(), reader.Name, reader.AttributeCount, reader.Value); 
                break;
        }
    }

    public void SetDisconnectCallback(Func<XmppClient, bool> callback)
    {
        _disconnectCallback = callback;
    }

    private void OnRedisMessage(ChannelMessage msg)
    {
        // Being here means the client should receive this information
        _logger.LogInformation(msg.ToString());
        var ms = RedisMessage.FromJson(msg.Message.ToString());
        if (ms == null)
        {
            _logger.LogCritical("Invalid Redis Message");
            return;
        };
        switch (ms.Type)
        {
            case "message":
                OnMessage(Message.FromJson(ms.Object));
                break;
            case "presence":
                OnPresence(Presence.FromJson(ms.Object));
                break;
            default:
                _logger.LogCritical("Unknown redis message type {type}", ms.Type);
                break;
        }
    }

    public void OnMessage(Message msg)
    {
        _writeQueue.Add(msg);
    }

    public void OnPresence(Presence msg)
    {
        _writeQueue.Add(msg);
    }
    
    /// <summary>
    /// Creates a new Message instance from the given XmlReader and publishes the result to Redis on the channel
    /// determined by the message's "to" attribute 
    /// </summary>
    /// <param name="reader">Client's XmlReader from readThread</param>
    /// <exception cref="Exception">Throws exception if the active client does not have a valid JID or
    /// if the read message does not contain a type, recipient, or is generally malformed</exception>
    private void OnMessageElement(XmlReader reader)
    {
        if (_jid == null || _jid.Jid.Length == 0) throw new Exception("invalid client JID");
        var msg = new Message(reader);
        var recipient = msg.GetRecipient();
        if (recipient == null)
        {
            _logger.LogCritical("Message does not contain a recipient!");
            return;
        }

        if (msg.GetSender() == null) msg.SetSender(_jid.Jid);
        _redis.GetSubscriber().Publish(recipient, new RedisMessage("message", msg.ToJson()).ToJson());
    }

    private void OnPresenceElement(XmlReader reader)
    {
        if (_jid == null || _jid.Jid.Length == 0) throw new Exception("invalid client JID");
        var presence = new Presence(reader);
        var recipient = presence.GetRecipient();
        if (recipient == null)
        {
            _logger.LogTrace("presence does not contain a recipient!");
            _presence = presence;
            return;
        }
        if (presence.GetSender() == null) presence.SetSender(_jid.Jid);
        _redis.GetSubscriber().Publish(recipient, new RedisMessage("presence", presence.ToJson()).ToJson());
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
                case "message":
                    OnMessageElement(reader);
                    break;
                case "presence":
                    OnPresenceElement(reader);
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
            case "set":
                OnSetIqStanza(reader);
                break;
            default:
                throw new XmlException("Invalid IQ Stanza type");
        } 
    }

    private void OnSetIqStanza(XmlReader reader)
    {
        var id = reader.GetAttribute("id");
        if (id == null)
        {
            throw new XmlException("invalid iq stanza id");
        }

        if (!reader.Read())
        {
            throw new XmlException("could not read IQ stanza contents");
        }

        switch (reader.Name)
        {
            case "bind":
                OnBindResource(reader, id);
                break;
            case "session":
                OnSession(reader, id);
                break;
            default:
                throw new XmlException("could not get iq stanza contents");
        }
    }

    private void OnSession(XmlReader reader, string id)
    {
        if (reader.NamespaceURI != "urn:ietf:params:xml:ns:xmpp-session")
        {
            throw new XmlException("session stanza has an invalid namespace");
        }

        if (id.Length == 0)
        {
            throw new ArgumentException("id cannot be a zero length");
        }

        var iqSession = new IqSessionElement(id);
        
        _writeQueue.Add(iqSession);
    }
    
    private void OnBindResource(XmlReader reader, string id)
    {
        if (reader.NamespaceURI != "urn:ietf:params:xml:ns:xmpp-bind")
        {
            throw new XmlException("bind stanza has an incorrect namespace");
        }

        if (_jid == null)
        {
            throw new AuthenticationException("invalid JID");
        }
        
        // Get what the resource is
        if (!reader.Read() && reader.Name != "resource")
        {
            throw new XmlException("could not read contained resource");
        }

        var iqJid = new IqJidElement(id, _jid.Jid, _clientId);
       
        SubscribeToChannel(_jid.Jid);

        reader.Read();
        _logger.LogInformation("Bound resource '{0}'", reader.Value);
        
        _writeQueue.Add(iqJid);
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
                var res = new RegisterResponseElement(id);
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
        var response = new SuccessElement();
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
            "http://etherx.jabber.org/streams", _clientId);
        _writeQueue.Add(stream);

        if (!_handshakeCompleted)
        {
            StartTlsElement tls = new StartTlsElement("stream");
            _writeQueue.Add(tls);
        }
        else
        {
            var features = new RegisterFeatures("stream");
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