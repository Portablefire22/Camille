using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Camille;

public class XmppClient
{
    private readonly string _clientId;

    private Thread _readThread;
    private Thread _writeThread;

    private readonly Stream _stream;
    private readonly TcpClient _client;

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

    public void Close()
    {
        _isRunning = false;
        if (_stream.CanRead || _stream.CanWrite)
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
        _readThread = new Thread(ReadLoop);
        _writeThread = new Thread(WriteLoop);

        _readThread.Start();
        _writeThread.Start();
    }

    private void ReadLoop()
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

    private void OnXmlElement(XmlReader reader)
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

    private bool Handshake()
    {
        StreamElement stream = new StreamElement("stream",
            "http://etherx.jabber.org/streams", 
            new XmlDocument());
        stream.ClientId = _clientId;
        
        _writeQueue.Add(stream);
        
        StartTlsElement tls = new StartTlsElement("stream",
            "http://etherx.jabber.org/streams", 
            new XmlDocument());
        _writeQueue.Add(tls);
        Console.WriteLine("Sent handshake response"); 
        return true;
    }

    private void WriteLoop()
    {
        using var writer = new XmppWriter(XmlWriter.Create(_stream));
        try
        {
            while (_isRunning)
            {
                while (_writeQueue.Count > 0)
                {
                    _writeQueue.First().Send(writer);
                    _writeQueue.RemoveAt(0);
                }
                Thread.Sleep(100);
            }
        }
        finally
        {
            writer.Close();
            _disconnectCallback(this);
        }
    }
}