using System.Xml;

namespace Camille;

public class RegisterFeatures(string prefix) : XmppElement()
{
    /// <summary>
    /// Sends the StreamElement to the XmppWriter's given stream. Closing
    /// the given writer will close the stream tag, closing the underlying stream.
    /// </summary>
    /// <param name="writer"></param>
    public override void Send(StreamWriter writer)
    {
        string xml = "<" + prefix + ":features>" + "<register xmlns=\"http://jabber.org/features/iq-register\"/>"
            + "</" + prefix + ":features>";
        writer.Write(xml);
    }
}