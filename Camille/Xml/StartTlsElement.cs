using System.Xml;

namespace Camille;

public class StartTlsElement(string? prefix, string? namespaceUri, XmlDocument doc) : XmppElement(prefix, "stream", namespaceUri, doc)
{
    public override void Send(StreamWriter writer)
    {
        
        string xml = "<" + Prefix + ":features>" 
            + "<mechanisms xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\">"
            + "<mechanism>PLAIN</mechanism>" 
            + "<mechanism>ANONYMOUS</mechanism>"
            + "<mechanism>EXTERNAL</mechanism>"
            + "</mechanisms>" 
        + "</" + Prefix + ":features>";
        writer.Write(xml);
    }
}