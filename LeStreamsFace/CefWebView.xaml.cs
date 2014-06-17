using System.ComponentModel;
using System.Runtime.CompilerServices;
using CefSharp;
using System;
using System.Windows;
using System.Windows.Controls;
using CefSharp.Wpf;

namespace LeStreamsFace
{
    /// <summary>
    /// Interaction logic for CefWebView.xaml
    /// </summary>
    public partial class CefWebView : UserControl, INotifyPropertyChanged
    {
        public CefWebView()
        {
            InitializeComponent();
            DataContext = this;

           

        }

        private IWpfWebBrowser webBrowser;
        private string _inputText;

        public IWpfWebBrowser WebBrowser
        {
            get { return webBrowser; }
            set { PropertyChanged.ChangeAndNotify(ref webBrowser, value, () => WebBrowser); }
        }
        public static void Init()
        {
            var settings = new CefSettings()
                           {
                               BrowserSubprocessPath = "CefSharp.BrowserSubprocess.exe"
                           };
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = CefSharpSchemeHandlerFactory.SchemeName,
                SchemeHandlerFactory = new CefSharpSchemeHandlerFactory()
            });
            Cef.Initialize(settings);


            //            CefSharp.Settings settings = new CefSharp.Settings();
            //            if (CEF.Initialize(settings))
            //            {
            //                CEF.RegisterScheme("theme", new ThemeSchemeHandlerFactory());
            //                //CEF.RegisterScheme("test", new SchemeHandlerFactory());
            //                //CEF.RegisterJsObject("bound", new BoundObject());
            //            }
        }

        public static string BaseDirectory { get; set; }

        #region public string Html

        public static DependencyProperty HtmlProperty =
            DependencyProperty.Register("Html", typeof(string), typeof(CefWebView), new PropertyMetadata(" ", HtmlChanged));

        public string Html
        {
            get { return (string)GetValue(HtmlProperty); }
            set { SetValue(HtmlProperty, value); }
        }

        #endregion public string Html

        #region public string FileName

        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register("FileName", typeof(string), typeof(CefWebView), new PropertyMetadata("", FileNameChanged));

        public string FileName
        {
            get { return (string)GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        #endregion public string FileName

        #region public double ScrollPercentage

        public static DependencyProperty ScrollPercentageProperty =
            DependencyProperty.Register("ScrollPercentage", typeof(double), typeof(CefWebView), new PropertyMetadata(0d, ScrollPercentageChanged));

        public string ScrollPercentage
        {
            get { return (string)GetValue(ScrollPercentageProperty); }
            set { SetValue(ScrollPercentageProperty, value); }
        }

        #endregion public double ScrollPercentage

        public void Print()
        {
//            if (webView.IsBrowserInitialized)
//                webView.Print();
        }

        private static void HtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var htmlPreview = (CefWebView)d;
            if (htmlPreview.webView != null && htmlPreview.webView.IsBrowserInitialized)
                htmlPreview.webView.LoadHtml((string)e.NewValue, "www.customHTML.com.net.org");
        }

        private static void FileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var htmlPreview = (CefWebView)d;
            if (htmlPreview.webView != null && htmlPreview.webView.IsBrowserInitialized)
                htmlPreview.webView.Title = (string)e.NewValue;
        }

        private static void ScrollPercentageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var htmlPreview = (CefWebView)d;
            if (htmlPreview.webView != null && htmlPreview.webView.IsBrowserInitialized)
            {
                var javascript = string.Format("window.scrollTo(0,{0} * (document.body.scrollHeight - document.body.clientHeight));", e.NewValue);
                htmlPreview.webView.EvaluateScript(javascript);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

//            webView.PropertyChanged += webView_PropertyChanged;
        }

        private void webView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (webView == null) return;

            switch (e.PropertyName)
            {
                case "IsBrowserInitialized":
                    if (webView.IsBrowserInitialized)
                        Dispatcher.BeginInvoke((Action)InitializeData);
                    break;
            }
        }

        private void InitializeData()
        {
            webView.LoadHtml(Html, "www.google.com");
            webView.Title = FileName;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            webBrowser.Address = InputText;

            var wings = @"<object type=""application/x-shockwave-flash"" height=""378"" width=""620"" id=""live_embed_player_flash"" data=""http://www.twitch.tv/widgets/live_embed_player.swf?channel=wingsofdeath"" bgcolor=""#000000""><param name=""allowFullScreen"" value=""true"" /><param name=""allowScriptAccess"" value=""always"" /><param name=""allowNetworking"" value=""all"" /><param name=""movie"" value=""http://www.twitch.tv/widgets/live_embed_player.swf"" /><param name=""flashvars"" value=""hostname=www.twitch.tv&channel=wingsofdeath&auto_play=true&start_volume=25"" /></object><a href=""http://www.twitch.tv/wingsofdeath"" style=""padding:2px 0px 4px; display:block; width:345px; font-weight:normal; font-size:10px;text-decoration:underline; text-align:center;"">Watch live video from Wingsofdeath on www.twitch.tv</a>";
            webView.LoadHtml(wings, wings);// = wings;
        }

        public string InputText
        {
            get { return _inputText; }
            set { PropertyChanged.ChangeAndNotify(ref _inputText, value, () => InputText); }
        }
    }
}