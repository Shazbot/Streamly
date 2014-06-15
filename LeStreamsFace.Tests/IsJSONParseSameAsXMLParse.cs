using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using ApprovalTests;
using ApprovalTests.Reporters;
using LeStreamsFace.StreamParsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }
}
