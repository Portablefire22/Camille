using System.Xml;

namespace Camille;

public class StartTlsElement(string? prefix) : XmppElement()
{
    public override void Send(StreamWriter writer)
    {
        
        string xml = "<" + prefix + ":features>" 
            + "<mechanisms xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\">"
            + "<mechanism>PLAIN</mechanism>" 
            + "<mechanism>ANONYMOUS</mechanism>"
            + "<mechanism>EXTERNAL</mechanism>"
            + "</mechanisms>" 
        + "</" + prefix + ":features>";
        writer.Write(xml);
    }
}