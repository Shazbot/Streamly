using CefSharp;
using CefSharp.Example;
using CefSharp.Wpf;
using LeStreamsFace.Annotations;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

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
            browser.IsBrowserInitializedChanged += BrowserOnIsBrowserInitializedChanged;
            DataContext = this;
        }

        private void BrowserOnIsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if ((bool)dependencyPropertyChangedEventArgs.NewValue)
            {
                Dispatcher.BeginInvoke((Action)InitializeData);
                browser.FrameLoadStart += BrowserOnFrameLoadStart;
            }
        }

        private void BrowserOnFrameLoadStart(object sender, FrameLoadStartEventArgs args)
        {
        }

        public string Address { get; set; }

        public string Title { get; set; }

        private IWpfWebBrowser webBrowser;

        public IWpfWebBrowser WebBrowser
        {
            get { return webBrowser; }
            //            set { webBrowser = value; }
            set { PropertyChanged.ChangeAndNotify(ref webBrowser, value, () => WebBrowser); }
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

        private string oldHtml;
        private bool loadedHtml;
        private string _html;

        public static List<CefWebView> views = new List<CefWebView>();

        private static void HtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var host = (CefWebView)d;
            if (!views.Contains(host)) views.Add(host); // check if we are reusing views in tabcontrol
            if (host.browser == null) return;

            //            var html = ((string)(e.NewValue)).Replace("width=\"100%\"", "width=\"" + host.ActualWidth + "\"").Replace("height=\"100%\"", "height=\"" + host.ActualHeight + "\"");
            var html = ((string)(e.NewValue));//.Replace("height=\"100%\"", "height=\"100%\"");
            //            if (html == host.oldHtml) return;
            //            if (host.loadedHtml) return;
            //            host.oldHtml = html;
            host.browser.LoadHtml(html, "url");
            //            host.loadedHtml = true;

            //            host.browser.LoadHtml(((string)e.NewValue), "url");
        }

        private void webView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (browser == null) return;

            switch (e.PropertyName)
            {
            }
        }

        private void InitializeData()
        {
            //            var html = @"<object type=""application/x-shockwave-flash"" height=""100%"" width=""100%"" style=""overflow:hidden; width:100%; height:100%; margin:0; padding:0; border:0;"" id=""live_embed_player_flash"" data=""http://www.twitch.tv/widgets/live_embed_player.swf?channel=hotshotgg"" bgcolor=""#000000""><param name=""allowFullScreen"" value=""true"" /><param name=""allowScriptAccess"" value=""always"" /><param name=""allowNetworking"" value=""all"" /><param name=""movie"" value=""http://www.twitch.tv/widgets/live_embed_player.swf"" /><param name=""flashvars"" value=""hostname=www.twitch.tv&channel=hotshotgg&auto_play=true&start_volume=25"" /></object>";
            //            browser.LoadHtml(Html, "url");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            //            Console.WriteLine("x is " + e.GetPosition(browser).X);
            //            Console.WriteLine("y is " + e.GetPosition(browser).Y);
            //            Console.WriteLine("b width is " + browser.Width + "       " + browser.ActualWidth);
            //            Console.WriteLine("b height is " + browser.Height + "       " + browser.ActualHeight);
        }

        private void Browser_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //            e.Handled = true;
        }

        private void Browser_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //            e.Handled = true;
        }
    }
}