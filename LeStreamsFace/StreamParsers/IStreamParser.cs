using System.Collections.Generic;
using System.Xml.Linq;

namespace LeStreamsFace.StreamParsers
{
    internal interface IStreamParser<T>
    {
        IEnumerable<Stream> GetStreamsFromContent(string content);

        Stream GetStreamFromElement(T xElement);
    }
}