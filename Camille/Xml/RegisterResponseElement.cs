using System.Xml;

namespace Camille;

public class RegisterResponseElement
    (string? prefix, string? namespaceUri, XmlDocument doc) : XmppElement(prefix, "stream", namespaceUri, doc)
{

    private string _id = "";

    public string Id
    {
        get => _id;
        set => _id = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Sends the StreamElement to the XmppWriter's given stream. Closing
    /// the given writer will close the stream tag, closing the underlying stream.
    /// </summary>
    /// <param name="writer"></param>
    public override void Send(StreamWriter writer)
    {
        string xml = "<iq type=\'result\' id='" + _id + "\'>"
            + "<query xmlns=\'jabber:iq:register\'>"
                //+ "<instructions/>"
            + "<username/>"
            + "<password/>"
            + "</query>"
            + "</iq>";
        writer.Write(xml);
    }
}