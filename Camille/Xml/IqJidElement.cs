using System.Xml;

namespace Camille;

public class IqJidElement(string id, string jid, string clientId) : XmppElement()
{
    public override void Send(StreamWriter writer)
    {
        string xml = $"<iq id='{id}' type='result'><bind xmlns='urn:ietf:params:xml:ns:xmpp-bind'>" +
                     $"<jid>{jid}/{clientId}</jid></bind></iq>";
        writer.Write(xml);
    }
}