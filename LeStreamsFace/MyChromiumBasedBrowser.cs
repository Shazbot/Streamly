using CefSharp.Wpf;

namespace LeStreamsFace
{
    internal class MyChromiumBasedBrowser : ChromiumWebBrowser
    {
        protected override void OnAddressChanged(string oldValue, string newValue)
        {
            if (newValue.ToUpperInvariant().Contains("TWITCH")) return;

            base.OnAddressChanged(oldValue, newValue);
        }
    }
}