using System.Xml;

namespace Camille;

public class StartTlsElement(string? prefix, string? namespaceUri, XmlDocument doc) : XmppElement(prefix, "stream", namespaceUri, doc)
{
    public override void Send(XmppWriter writer)
    {
        writer.WriteStartElement(Prefix, "features", NamespaceURI);
        writer.WriteStartElement("starttls");
        writer.WriteAttributeString("xmnls", "urn:ietf:params:xml:ns:xmpp-tls");
        writer.WriteStartElement( "required");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteStartElement("mechanisms");
        writer.WriteAttributeString("xmnls", "urn:ietf:params:xml:ns:xmpp-sasl");
        writer.WriteStartElement("mechanism");
        writer.WriteValue("DIGEST-MD5");
        writer.WriteEndElement();
        writer.WriteStartElement("mechanism");
        writer.WriteValue("PLAIN");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.Flush();
    }
}