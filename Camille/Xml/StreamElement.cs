using System.Xml;

namespace Camille;

public class StreamElement(string? prefix, string? namespaceUri, XmlDocument doc) : XmppElement(prefix, "stream", namespaceUri, doc)
{
    public string? ClientId
    {
        get => _clientId;
        set => _clientId = value ?? throw new ArgumentNullException(nameof(value));
    }

    private string? _clientId;
    
    
    /// <summary>
    /// Sends the StreamElement to the XmppWriter's given stream. Closing
    /// the given writer will close the stream tag, closing the underlying stream.
    /// </summary>
    /// <param name="writer"></param>
    public override void Send(StreamWriter writer)
    {
        string xml = "<" + Prefix + ":stream "
                     + "xmlns=\"jabber:client\" "
                     + " xmlns:" + Prefix + "=\"" + NamespaceURI + "\" "
                     + "from=\"pvp.net\" "
                     + "version=\"1.0\" ";
        if (_clientId != null)
        {
            xml += "id=\"" + ClientId + "\"";
        }
        xml += ">"; 
        
        writer.Write(xml);
    }
}