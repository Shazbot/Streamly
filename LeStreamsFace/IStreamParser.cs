using System.Xml.Linq;

namespace LeStreamsFace
{
    internal interface IStreamParser
    {
        Stream GetStreamFromXElement(XElement xElement);
    }
}