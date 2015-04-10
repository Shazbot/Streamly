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
using System.IO.Packaging;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xunit;

namespace LeStreamsFace.Tests
{
    public class IsJSONParseSameAsXMLParse
    {
        [UseReporter(typeof(DiffReporter))]
        [Fact]
        public void XMLParseApproval()
        {
            // https://stackoverflow.com/questions/3710776/pack-urls-and-unit-testing-problem-with-my-environment
            Application.ResourceAssembly = typeof(App).Assembly;

            var input = File.ReadAllText(@"data/streams online.xml");

            var parserXML = new TwitchXMLStreamParser();
            var streams = parserXML.GetStreamsFromContent(input);

            Approvals.VerifyAll(streams, "");
        }

        [UseReporter(typeof(DiffReporter))]
        [Fact]
        public void JSONParseApproval()
        {
            // https://stackoverflow.com/questions/3710776/pack-urls-and-unit-testing-problem-with-my-environment
            Application.ResourceAssembly = typeof(App).Assembly;

            var input = File.ReadAllText(@"data/streams online.json");

            var parseJSON = new TwitchJSONStreamParser();
            var streams = parseJSON.GetStreamsFromContent(input);

            Approvals.VerifyAll(streams, "");
        }
    }
}