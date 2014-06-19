using CefSharp;
using CefSharp.Example;
using CefSharp.Wpf;
using LeStreamsFace.Annotations;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LeStreamsFace
{
    [ImplementPropertyChanged]
    public partial class CefWebView : UserControl, INotifyPropertyChanged
    {
        public CefWebView()
        {
            InitializeComponent();
            browser.RequestHandler = new RequestHandler();

            PropertyChanged += webView_PropertyChanged;
            browser.IsBrowserInitializedChanged += (sender, args) => Dispatcher.BeginInvoke((Action)InitializeData);
            DataContext = this;

//            var html = @"<object type=""application/x-shockwave-flash"" height=""100%"" width=""100%"" style=""overflow:hidden; width:100%; height:100%; margin:0; padding:0; border:0;"" id=""live_embed_player_flash"" data=""http://www.twitch.tv/widgets/live_embed_player.swf?channel=hotshotgg"" bgcolor=""#000000""><param name=""allowFullScreen"" value=""true"" /><param name=""allowScriptAccess"" value=""always"" /><param name=""allowNetworking"" value=""all"" /><param name=""movie"" value=""http://www.twitch.tv/widgets/live_embed_player.swf"" /><param name=""flashvars"" value=""hostname=www.twitch.tv&channel=hotshotgg&auto_play=true&start_volume=25"" /></object>";
//            browser.LoadHtml(html, "a");
//	    	    browser.WebBrowser.LoadHtml(html,"url");

        }

        public string Address { get; set; }

        public string Title { get; set; }

        private IWpfWebBrowser webBrowser;

        public IWpfWebBrowser WebBrowser
        {
            get { return webBrowser; }
            set { webBrowser = value; }
//            set { PropertyChanged.ChangeAndNotify(ref webBrowser, value, () => WebBrowser); }
        }

        #region public string Html

        public static DependencyProperty HtmlProperty =
            DependencyProperty.Register("Html", typeof(string), typeof(CefWebView), new PropertyMetadata(" ", HtmlChanged));

        public string Html
        {
            get { return (string)GetValue(HtmlProperty); }
            set
            {
                SetValue(HtmlProperty, value);
            }
        }

        #endregion public string Html

        private static void HtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (CefWebView)d;

	    host.browser.LoadHtml((string)e.NewValue, "url");
//            //            htmlPreview.webView.LoadHtml((string)e.NewValue, "url");
//
//            // old cp'd code
//            // does this even fire
//            // IsBrowserInitialized should maybe be webView.WebBrowser != null
//            if (browser != null && htmlPreview.webView.IsBrowserInitialized)
//                htmlPreview.webView.LoadHtml((string)e.NewValue, "url");
        }

        private void webView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (browser == null) return;

            var isBrowserInitialized = browser.IsBrowserInitialized;

            switch (e.PropertyName)
            {
                case "IsBrowserInitialized":
                    if (browser.IsBrowserInitialized)
                        Dispatcher.BeginInvoke((Action)InitializeData);
                    break;
            }
        }

        private void InitializeData()
        {
            var html = @"<object type=""application/x-shockwave-flash"" height=""100%"" width=""100%"" style=""overflow:hidden; width:100%; height:100%; margin:0; padding:0; border:0;"" id=""live_embed_player_flash"" data=""http://www.twitch.tv/widgets/live_embed_player.swf?channel=hotshotgg"" bgcolor=""#000000""><param name=""allowFullScreen"" value=""true"" /><param name=""allowScriptAccess"" value=""always"" /><param name=""allowNetworking"" value=""all"" /><param name=""movie"" value=""http://www.twitch.tv/widgets/live_embed_player.swf"" /><param name=""flashvars"" value=""hostname=www.twitch.tv&channel=hotshotgg&auto_play=true&start_volume=25"" /></object>";
//            browser.LoadHtml(html, "a");
//            browser.Address = "www.google.com";

//            webBrowser.Address = "www.google.com";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}