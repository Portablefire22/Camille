using System.Xml;

namespace Camille;

public abstract class XmppElement()
{
    
    public abstract void Send(StreamWriter writer);
}