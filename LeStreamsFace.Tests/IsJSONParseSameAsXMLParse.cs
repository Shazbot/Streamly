using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using ApprovalTests;
using ApprovalTests.Reporters;
using FluentAssertions;
using LeStreamsFace.StreamParsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RestSharp;
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
        public async void DoXMLandJSONparseGiveTheSameResult()
        {
            var XMLparser = new TwitchXMLStreamParser();
            var twitchXMLResponse = new RestClient("http://api.justin.tv/api/stream/list.xml?category=gaming&limit=100").ExecuteTaskAsync(new RestRequest());

            var JSONparser = new TwitchJSONStreamParser();
            var twitchJSONResponse = new RestClient("https://api.twitch.tv/kraken/streams?limit=100").SinglePageResponse();
            var streamsFromJSON = JSONparser.GetStreamsFromContent(twitchJSONResponse.Content);

            var streamsFromXML = XMLparser.GetStreamsFromContent((await twitchXMLResponse).Content);
            streamsFromXML = streamsFromXML.Skip(1);

//            streamsFromJSON.Count().ShouldBe(streamsFromXML.Count()-1);
//            streamsFromJSON.ShouldBe(streamsFromXML);

//            (new[] { 1, 2, 3 }).ShouldBe(new[] { 1, 2, 4 });
            (new[] { 1, 2, 3 }).Should().BeSameAs(new[] { 1, 2, 4 });
        }
    }
}
