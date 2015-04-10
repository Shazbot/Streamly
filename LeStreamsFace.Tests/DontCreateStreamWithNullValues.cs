using FluentAssertions;
using LeStreamsFace.StreamParsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Windows;
using Xunit;

namespace LeStreamsFace.Tests
{
    public class DontCreateStreamWithNullValues
    {
        [Fact]
        private void JSONInputHasNullValues()
        {
            // https://stackoverflow.com/questions/3710776/pack-urls-and-unit-testing-problem-with-my-environment
            Application.ResourceAssembly = typeof(App).Assembly;

            var input = File.ReadAllText(@"data/streams online.json");
            var streamKeys = new[] { "_id", "viewers", "game" };
            var channelKeys = new[] { "_id", "name", "video_banner", "display_name", "status" };

            var random = new Random();

            var streams = (JArray)JObject.Parse(input)["streams"];

            // sprinkle null values in JSON
            foreach (var stream in streams)
            {
                switch (random.Next(2))
                {
                    case 0:
                        stream[streamKeys[random.Next(2)]] = null;
                        break;

                    case 1:
                        stream["channel"][channelKeys[random.Next(5)]] = null;
                        break;
                }
            }

            var messedWithInput = new JObject();
            messedWithInput["streams"] = streams;

            var parseJSON = new TwitchJSONStreamParser();
            var gameStreams = parseJSON.GetStreamsFromContent(messedWithInput.ToString());

            gameStreams.Should().NotBeEmpty();
        }
    }
}