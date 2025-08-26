using System.Xml;

namespace Camille;

public class IqJidElement(string? prefix, string? namespaceUri, XmlDocument doc) : XmppElement(prefix, "stream", namespaceUri, doc)
{
    private string _id;
    private string _jid;
    private string _clientId;

    public string Id
    {
        get => _id;
        set => _id = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Jid
    {
        get => _jid;
        set => _jid = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string ClientId
    {
        get => _clientId;
        set => _clientId = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override void Send(StreamWriter writer)
    {
        string xml = $"<iq id='{_id}' type='result'><bind xmlns='urn:ietf:params:xml:ns:xmpp-bind'>" +
                     $"<jid>{_jid}/{_clientId}</jid></bind></iq>";
        writer.Write(xml);
    }
}