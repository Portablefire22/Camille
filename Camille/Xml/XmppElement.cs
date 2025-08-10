using System.Xml;

namespace Camille;

public abstract class XmppElement(string? prefix, string localName, string? namespaceUri, XmlDocument doc)
    : XmlElement(prefix, localName, namespaceUri, doc)
{
    
    public abstract void Send(XmppWriter writer);
}