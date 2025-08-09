using System.Xml;

namespace Camille;

public class XmppElement(string? prefix, string localName, string? namespaceUri, XmlDocument doc)
    : XmlElement(prefix, localName, namespaceUri, doc);