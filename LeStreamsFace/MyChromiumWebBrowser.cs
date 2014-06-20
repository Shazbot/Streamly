using CefSharp.Wpf;

namespace LeStreamsFace
{
    internal class MyChromiumWebBrowser : ChromiumWebBrowser
    {
        protected override void OnAddressChanged(string oldValue, string newValue)
        {
            base.OnAddressChanged(oldValue, newValue);
        }
    }
}