using System.Collections.Generic;
using System.Xml.Linq;

namespace LeStreamsFace.StreamParsers
{
    internal interface IStreamParser
    {
        IEnumerable<Stream> GetStreamsFromContent(string content);

        Stream GetStreamFromXElement(XElement xElement);
    }
}