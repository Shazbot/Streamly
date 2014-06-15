using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Deserializers;

namespace LeStreamsFace.StreamParsers
{
    class TwitchJSONStreamParser : IStreamParser<JToken>
    {
        public IEnumerable<Stream> GetStreamsFromContent(string content)
        {
            var streams = new List<Stream>();
            foreach (var jObject in JsonConvert.DeserializeObject<JObject>(content)["streams"])
            {
                streams.Add(GetStreamFromElement(jObject));
            }
            return streams;
        }

        public Stream GetStreamFromElement(JToken jObject)
        {
            var channelObject = jObject["channel"];
            // so we don't have to make setters for all these fields in stream
            jObject["Name"] = channelObject["display_name"];
            jObject["Title"] = channelObject["status"];
            jObject["ChannelId"] = channelObject["_id"];
            jObject["LoginNameTwtv"] = channelObject["name"];
            jObject["ThumbnailURI"] = channelObject["video_banner"];
            return jObject.ToObject<Stream>();
        }
    }
}