using System.Xml;

namespace Camille;

public class SuccessElement(string? prefix, string? namespaceUri, XmlDocument doc) : XmppElement(prefix, "stream", namespaceUri, doc)
{
    /// <summary>
    /// Sends the StreamElement to the XmppWriter's given stream. Closing
    /// the given writer will close the stream tag, closing the underlying stream.
    /// </summary>
    /// <param name="writer"></param>
    public override void Send(StreamWriter writer)
    {
        string xml = "<success xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\"/>";
        writer.Write(xml);
    }
}