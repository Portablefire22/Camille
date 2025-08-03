using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Camille;

public class XMPPClient
{
    public string _clientId;

    private Thread _readThread;
    private Thread _writeThread;

    private Stream _stream;
    private TcpClient _client;

    private List<XmlElement> _writeQueue;

    public XMPPClient(TcpClient client, Stream stream)
    {
        _clientId = Guid.NewGuid().ToString();
        _stream = stream;
        _client = client;
        CreateThreads(stream);
    }

    void CreateThreads(Stream stream)
    {
        _readThread = new Thread(ReadLoop);
        _writeThread = new Thread(WriteLoop);

        _readThread.Start();
        _writeThread.Start();
    }

    void ReadLoop()
    {
        var settings = new XmlReaderSettings
        {
            ConformanceLevel = ConformanceLevel.Fragment
        };
        using (var reader = XmlReader.Create(_stream, settings))
        {
            while (reader.Read())
            {
                OnXmlNode(reader);
            }
        }
    }

    void OnXmlNode(XmlReader reader)
    {
        switch (reader.NodeType)
        {
            // Just ignore this for now
            case XmlNodeType.XmlDeclaration:
                break;
            case XmlNodeType.Element:
                OnXmlElement(reader);
                break;
            default:
                Console.WriteLine("Unknown node: {0}, {1}, {2}, {3}", reader.NodeType, reader.Name, reader.Name, reader.AttributeCount); 
                break;
        }
    }

    void OnXmlElement(XmlReader reader)
    {
        
    }
    
    void WriteLoop()
    {
        while (true)
        {
            
        }
    }
}