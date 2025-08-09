using System.Xml;

namespace Camille;

public class StreamElement(string? prefix, string localName, string? namespaceUri, XmlDocument doc) : XmppElement(prefix, localName, namespaceUri, doc)
{
}