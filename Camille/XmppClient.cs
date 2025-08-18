using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Camille;

public class XmppClient
{
    private string _clientId;

    private Thread _readThread;
    private Thread _writeThread;

    private readonly Stream _stream;
    private readonly TcpClient _client;

    private bool _handshakeCompleted = false;
    private bool _isRunning;

    
    private Func<XmppClient, bool> _disconnectCallback;
    
    private List<XmppElement> _writeQueue;

    public XmppClient(TcpClient client, Stream stream)
    {
        _clientId = Guid.NewGuid().ToString();
        _stream = stream;
        _client = client;
        _writeQueue = [];
        CreateThreads(stream);
    }

    public void Close(bool closeStream)
    {
        _isRunning = false;
        if (closeStream && (_stream.CanRead || _stream.CanWrite))
        {
            _stream.Close();
        }
        if (_client.Connected)
        {
            _client.Close();
        }
    }
    
    public string GetClientId()
    {
        return _clientId;
    }

    private void CreateThreads(Stream stream)
    {
        _isRunning = true;
        
        _writeThread = new Thread(WriteLoop);
        _readThread = new Thread(ReadLoop);
        
        _readThread.Start();
        _writeThread.Start();
    }

    private void OnXmlNode(XmlReader reader)
    {
        #if DEBUG 
                Console.WriteLine(reader.NodeType); 
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
                Console.WriteLine("Unknown node: {0}, {1}, {2}, {3}", reader.NodeType.ToString(), reader.Name, reader.Name, reader.AttributeCount); 
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
            Console.WriteLine("{0}, {1}, {2}, {3}", reader.NodeType.ToString(), reader.Value, reader.NamespaceURI, reader.Name);
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
                    Console.WriteLine("Name: {0}", reader.Name);
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
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
                Console.WriteLine("{0}, {1}, {2}, {3}", reader.NodeType.ToString(), reader.Value, reader.NamespaceURI, reader.Name);
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
            Console.WriteLine(e.ToString());
        }
    }

    private void OnAuthElement(XmlReader element)
    {
        var response = new SuccessElement(null, null, new XmlDocument());
        _handshakeCompleted = true;
        _writeQueue.Add(response);
    }

    private bool Handshake()
    {
        _readThread.Interrupt();
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
        Console.WriteLine("Sent handshake response"); 
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
                Console.WriteLine(e);
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
            Console.WriteLine(e.ToString());
        }
        finally
        {
            writer.Close();
            _disconnectCallback(this);
        }
    }
}