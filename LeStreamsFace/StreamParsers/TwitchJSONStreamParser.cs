using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace LeStreamsFace.StreamParsers
{
    internal class TwitchJSONStreamParser : IStreamParser<JToken>
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
            jObject["Name"] = channelObject["display_name"] ?? "";
            jObject["Title"] = channelObject["status"] ?? "";
            jObject["ChannelId"] = channelObject["_id"] ?? "";
            jObject["LoginNameTwtv"] = channelObject["name"] ?? "";

            jObject["ThumbnailURI"] = jObject["preview"]["large"] ?? "";

            var jsonSerializerSettings = new JsonSerializerSettings()
                                         {
                                             NullValueHandling = NullValueHandling.Ignore,
                                             Error = SerializerErrorHandler
                                         };
            return jObject.ToObject<Stream>(JsonSerializer.Create(jsonSerializerSettings));
        }

        private void SerializerErrorHandler(object sender, ErrorEventArgs errorEventArgs)
        {
            var eventArgs = errorEventArgs;
        }
    }
}