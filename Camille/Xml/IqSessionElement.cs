using System.Xml;

namespace Camille;

public class IqSessionElement(string id) : XmppElement()
{
    public override void Send(StreamWriter writer)
    {
        string xml = $"<iq id='{id}' type='result'/>";
        writer.Write(xml);
    }
}