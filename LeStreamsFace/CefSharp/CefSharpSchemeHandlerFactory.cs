using CefSharp;

namespace LeStreamsFace
{
    internal class CefSharpSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public const string SchemeName = "custom";

        public ISchemeHandler Create()
        {
            return new CefSharpSchemeHandler();
        }
    }
}