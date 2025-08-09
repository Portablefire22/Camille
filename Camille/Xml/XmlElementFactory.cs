using System.Diagnostics;
using System.Xml;

namespace Camille;

public class XmlElementFactory
{

    public XmlElement CreateElement(XmlReader reader)
    {
        XmlElement ele;
        try
        {
            XmlDocument doc = new XmlDocument();
            ele = doc.CreateElement(reader.Name);
            ele.InnerXml = reader.ReadInnerXml();
        }
        catch (Exception e)
        {
            throw;
        }

        return ele;
    }
}