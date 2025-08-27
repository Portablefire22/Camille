using System.Xml;

namespace Camille;

public class BlankFeaturesElement(string prefix) : XmppElement()
{
    /// <summary>
    /// Sends the StreamElement to the XmppWriter's given stream. Closing
    /// the given writer will close the stream tag, closing the underlying stream.
    /// </summary>
    /// <param name="writer"></param>
    public override void Send(StreamWriter writer)
    {
        string xml = "<" + prefix + ":features/>";
        writer.Write(xml);
    }
}