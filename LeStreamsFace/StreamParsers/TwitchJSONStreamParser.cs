using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace LeStreamsFace.StreamParsers
{
    class TwitchJSONStreamParser : IStreamParser
    {
        public IEnumerable<Stream> GetStreamsFromContent(string content)
        {
            return Enumerable.Empty<Stream>();
        }

        public Stream GetStreamFromXElement(XElement xElement)
        {
            return null;
        }
    }
}