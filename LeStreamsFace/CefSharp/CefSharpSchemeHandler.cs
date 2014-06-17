using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CefSharp;

namespace LeStreamsFace
{
    internal class CefSharpSchemeHandler : ISchemeHandler
    {
        private readonly IDictionary<string, string> resources;

        public CefSharpSchemeHandler()
        {
            resources = new Dictionary<string, string>
                        {

                        };
        }

        public bool ProcessRequestAsync(IRequest request, ISchemeHandlerResponse response, OnRequestCompletedHandler requestCompletedCallback)
        {
            // The 'host' portion is entirely ignored by this scheme handler.
            var uri = new Uri(request.Url);
            var fileName = uri.AbsolutePath;

            string resource;
            if (resources.TryGetValue(fileName, out resource) &&
                !String.IsNullOrEmpty(resource))
            {
                var bytes = Encoding.UTF8.GetBytes(resource);
                response.ResponseStream = new MemoryStream(bytes);
                response.MimeType = GetMimeType(fileName);
                requestCompletedCallback();

                return true;
            }

            return false;
        }

        private string GetMimeType(string fileName)
        {
            if (fileName.EndsWith(".css")) return "text/css";
            if (fileName.EndsWith(".js")) return "text/javascript";

            return "text/html";
        }
    }
}