using System.Xml;

namespace Camille;

public class IqSessionElement(string? prefix, string? namespaceUri, XmlDocument doc) : XmppElement(prefix, "stream", namespaceUri, doc)
{
    private string _id;

    public string Id
    {
        get => _id;
        set => _id = value ?? throw new ArgumentNullException(nameof(value));
    }


    public override void Send(StreamWriter writer)
    {
        string xml = $"<iq id='{_id}' type='result'/>";
        writer.Write(xml);
    }
}