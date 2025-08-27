using System.Xml;

namespace Camille;

public class StreamElement(string? prefix, string? namespaceUri, string? clientId) : XmppElement()
{
    /// <summary>
    /// Sends the StreamElement to the XmppWriter's given stream. Closing
    /// the given writer will close the stream tag, closing the underlying stream.
    /// </summary>
    /// <param name="writer"></param>
    public override void Send(StreamWriter writer)
    {
        string xml = "<" + prefix + ":stream "
                     + "xmlns=\"jabber:client\" "
                     + " xmlns:" + prefix + "=\"" + namespaceUri + "\" "
                     + "from=\"pvp.net\" "
                     + "version=\"1.0\" ";
        if (clientId != null)
        {
            xml += "id=\"" + clientId + "\"";
        }
        xml += ">"; 
        
        writer.Write(xml);
    }
}