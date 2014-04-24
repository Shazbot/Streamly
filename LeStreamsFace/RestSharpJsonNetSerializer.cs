using Newtonsoft.Json;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serializers;
using System.IO;

namespace TwitchTesting
{
    internal class RestSharpJsonNetSerializer : ISerializer, IDeserializer
    {
        private readonly Newtonsoft.Json.JsonSerializer _serializer;

        public RestSharpJsonNetSerializer()
        {
            ContentType = "application/json";
            _serializer = new Newtonsoft.Json.JsonSerializer
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include
            };
        }

        public RestSharpJsonNetSerializer(Newtonsoft.Json.JsonSerializer serializer)
        {
            ContentType = "application/json";
            _serializer = serializer;
        }

        public string Serialize(object obj)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var jsonTextWriter = new JsonTextWriter(stringWriter))
                {
                    jsonTextWriter.Formatting = Formatting.Indented;
                    jsonTextWriter.QuoteChar = '"';

                    _serializer.Serialize(jsonTextWriter, obj);

                    var result = stringWriter.ToString();
                    return result;
                }
            }
        }

        public T Deserialize<T>(IRestResponse response)
        {
            using (var stringReader = new StringReader(response.Content))
            {
                using (var jsonTextReader = new JsonTextReader(stringReader))
                {
                    var obj = _serializer.Deserialize<T>(jsonTextReader);
                    return obj;
                }
            }
        }

        public string RootElement { get; set; }

        public string Namespace { get; set; }

        public string DateFormat { get; set; }

        public string ContentType { get; set; }
    }
}