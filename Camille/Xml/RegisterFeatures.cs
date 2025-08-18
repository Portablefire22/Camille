using System.Xml;

namespace Camille;

public class RegisterFeatures(string? prefix, string? namespaceUri, XmlDocument doc) : XmppElement(prefix, "stream", namespaceUri, doc)
{
    /// <summary>
    /// Sends the StreamElement to the XmppWriter's given stream. Closing
    /// the given writer will close the stream tag, closing the underlying stream.
    /// </summary>
    /// <param name="writer"></param>
    public override void Send(StreamWriter writer)
    {
        string xml = "<" + Prefix + ":features>" + "<register xmlns=\"http://jabber.org/features/iq-register\"/>"
            + "</" + Prefix + ":features>";
        writer.Write(xml);
    }
}