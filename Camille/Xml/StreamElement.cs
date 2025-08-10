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
    public override void Send(XmppWriter writer)
    {
        writer.WriteStartElement(Prefix, "stream", NamespaceURI);
        writer.WriteAttributeString("from", "pvp.net");
        writer.WriteAttributeString("xmlns", "jabber:client");
        writer.WriteAttributeString("version", "1.0");
        writer.WriteAttributeString("id", _clientId); 
        writer.Flush();
    }
}