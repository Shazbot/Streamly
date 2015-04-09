using ApprovalTests;
using ApprovalTests.Reporters;
using FluentAssertions;
using LeStreamsFace.StreamParsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LeStreamsFace.Tests
{
    public class IsJSONParseSameAsXMLParse
    {
        [UseReporter(typeof(DiffReporter))]
        [Fact]
        public void XMLParseApproval()
        {
            var input = File.ReadAllText(@"data/streams online.xml");

            var parserXML = new TwitchXMLStreamParser();
            var streams = parserXML.GetStreamsFromContent(input);

            Approvals.VerifyAll(streams, "");
        }

        [UseReporter(typeof(DiffReporter))]
        [Fact]
        public void JSONParseApproval()
        {
            var input = File.ReadAllText(@"data/streams online.json");

            var parseJSON = new TwitchJSONStreamParser();
            var streams = parseJSON.GetStreamsFromContent(input);

            Approvals.VerifyAll(streams, "");
        }

        [Fact]
        public async Task DoXMLandJSONparseGiveTheSameResult()
        {
            var XMLparser = new TwitchXMLStreamParser();
            var twitchXMLResponse = new RestClient("http://api.justin.tv/api/stream/list.xml?category=gaming&limit=100").ExecuteTaskAsync(new RestRequest());

            var JSONparser = new TwitchJSONStreamParser();
            var twitchJSONResponse = new RestClient("https://api.twitch.tv/kraken/streams?limit=100").SinglePageResponse();

            var streamsFromJSON = JSONparser.GetStreamsFromContent(twitchJSONResponse.Content).OrderByDescending(stream => stream.Viewers).ToArray();
            var streamsFromXML = XMLparser.GetStreamsFromContent((await twitchXMLResponse).Content).OrderByDescending(stream => stream.Viewers).ToArray();

            streamsFromJSON.Take(50).Should().BeSubsetOf(streamsFromXML);
        }

        //        [Fact(Skip = "Can't handle 9000 streams")]
        [Fact]
        public async Task CanWeGetAllStreams()
        {
            var JSONparser = new TwitchJSONStreamParser();
            var client = new RestClient("https://api.twitch.tv/kraken/streams?limit=100");
            client.AddHandler("application/json", new RestSharpJsonNetSerializer());
            var response = await client.ExecuteTaskAsync<JObject>(new RestRequest());
            var numStreams = response.Data["_total"].ToObject<int>();

            int numStreamsProcessed = 0;
            var tasks = new List<Task<string>>();
            do
            {
                numStreamsProcessed += 100;
                var newTask = Task.Run(async () =>
                                             {
                                                 var newClient = new RestClient("https://api.twitch.tv/kraken/streams?limit=100&offset=" + numStreamsProcessed);
                                                 var newResponse = await newClient.ExecuteTaskAsync(new RestRequest());
                                                 return newResponse.Content;
                                             });
                tasks.Add(newTask);
            } while (numStreamsProcessed < numStreams);//1000); // not using numStreams

            Task.WaitAll(tasks.ToArray());

            var streams = JSONparser.GetStreamsFromContent(response.Content).ToList();
            streams.AddRange(tasks.SelectMany(task => JSONparser.GetStreamsFromContent(task.Result)));
        }
    }
}