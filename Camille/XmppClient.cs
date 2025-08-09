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

    private Stream _stream;
    private TcpClient _client;

    private bool _isRunning;
    
    private Func<XmppClient, bool> _disconnectCallback;
    
    private List<XmlElement> _writeQueue;

    public XmppClient(TcpClient client, Stream stream)
    {
        _clientId = Guid.NewGuid().ToString();
        _stream = stream;
        _client = client;
        CreateThreads(stream);
    }

    public void Close()
    {
        _isRunning = false;
        _stream.Close();
        _client.Close();
    }
    
    public string GetClientId()
    {
        return _clientId;
    }

    void CreateThreads(Stream stream)
    {
        _isRunning = true;
        _readThread = new Thread(ReadLoop);
        _writeThread = new Thread(WriteLoop);

        _readThread.Start();
        _writeThread.Start();
    }

    void ReadLoop()
    {
        try
        {
            var settings = new XmlReaderSettings
            {
                ConformanceLevel = ConformanceLevel.Fragment
            };
            using (var reader = XmlReader.Create(_stream, settings))
            {
                while (reader.Read() && _isRunning)
                {
                    OnXmlNode(reader);
                }
            }
        }
        catch (Exception e)
        {   
            #if (DEBUG)
                Console.WriteLine(e.ToString());
            #endif
            _disconnectCallback(this);
        }
    }

    void OnXmlNode(XmlReader reader)
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
            default:
                Console.WriteLine("Unknown node: {0}, {1}, {2}, {3}", reader.NodeType.ToString(), reader.Name, reader.Name, reader.AttributeCount); 
                break;
        }
    }

    public void SetDisconnectCallback(Func<XmppClient, bool> callback)
    {
        _disconnectCallback = callback;
    }

    void OnXmlElement(XmlReader reader)
    {
        #if DEBUG 
            Console.WriteLine("{0}, {1}, {2}, {3}", reader.NodeType.ToString(), reader.Value, reader.NamespaceURI, reader.Name);
        #endif
        try
        {
            if (reader.Name == "stream:stream")
            {
                if (!Handshake())
                {
                    _disconnectCallback(this);
                }
                return;
            }
            var factory = new XmlElementFactory();
            var element = factory.CreateElement(reader);
            switch (element.Name)
            {
                case "stream:stream":
                    Console.WriteLine("Stream");
                    break;    
                default: 
                    Console.WriteLine("Name: {0}", element.Name);
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    bool Handshake()
    {
        var settings = new XmlWriterSettings();
        settings.CloseOutput = false;
        settings.WriteEndDocumentOnClose = false;

        using (XmlWriter w = XmlWriter.Create(_stream, settings))
        using (XmppWriterDecorator writer = new XmppWriterDecorator(w))
        {
            writer.WriteStartElement("stream", "stream", "http://etherx.jabber.org/streams");
            writer.WriteAttributeString("from", "pvp.net");
            writer.WriteAttributeString("xmlns", "jabber:client");
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("id", _clientId);
        }
        
        Console.WriteLine("Valid handshake"); 
        return true;
    }
    void WriteLoop()
    {
        while (_isRunning)
        {
            
        }
    }
}