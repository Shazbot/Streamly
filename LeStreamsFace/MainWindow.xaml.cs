using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Xml.Linq;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.MessageBox;

namespace LeStreamsFace
{
    internal partial class MainWindow : Window
    {
        private readonly System.Timers.Timer timer;
        private readonly DispatcherTimer dispatcherTimer;
        public static StreamsListWindow streamsWindow;
        public static bool WasTimeBlocking = false;
        private bool firstRun = true;

        public static readonly Taskbar Taskbar = new Taskbar();
        public static readonly List<NotificationWindow> NotificationWindows = new List<NotificationWindow>();

        private readonly IconWindow iconWindow;

        private readonly List<Stream> newStreamsList = new List<Stream>();
        private readonly List<Stream> unreportedFavs = new List<Stream>();
        private readonly List<Stream> closedLastPass = new List<Stream>();

        public delegate void ExitDelegate(object sender, EventArgs e);

        public delegate bool TimeBlockingDelegate();

        public MainWindow()
        {
#if DEBUG
            TextWriterTraceListener traceListener = new TextWriterTraceListener(System.IO.File.CreateText("Trace.txt"));
            Debug.Listeners.Add(traceListener);
            Debug.AutoFlush = true;
#endif

            InitializeComponent();
            this.Visibility = Visibility.Hidden;
            this.Show();

            try
            {
                ConfigManager.ReadConfigXml();

                ExitDelegate exitDelegate = new ExitDelegate(OnTrayExitClick);

                iconWindow = new IconWindow(exitDelegate);

                List<MenuItem> menuItems = new List<MenuItem>(iconWindow.notificationItem.MenuItems);
                menuItems.Insert(0, new MenuItem("-"));

                iconWindow.notificationItem.TrayContextMenu = new ContextMenu(menuItems.ToArray());
                iconWindow.notificationItem.TrayContextMenu.MenuItems.Add(0, new MenuItem("Stream list", StreamListOnClick));

                iconWindow.MouseLeftButtonDown += IconWindowOnMouseLeftButtonDown;
                iconWindow.Show();

                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 2500);
                dispatcherTimer.Tick += FullscreenWait;

                timer = new System.Timers.Timer(ConfigManager.SamplingInterval * 1000);
                timer.AutoReset = false;
                timer.Elapsed += mainTimer_Tick;

                mainTimer_Tick(null, null);
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                App.ExitApp();
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                App.ExitApp();
            }
            catch (FormatException e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                App.ExitApp();
            }
            catch (InvalidOperationException e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                App.ExitApp();
            }
            catch (IndexOutOfRangeException e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                App.ExitApp();
            }
        }

        private void FullscreenWait(object sender, EventArgs e)
        {
            if (DuringTimeBlock())
            {
                dispatcherTimer.Stop();
                return;
            }

            if (IsFullscreenAppRunning())
            {
                return;
            }

            dispatcherTimer.Stop();

            // remove duplicates in unreportedFavs, and display currently existing favorites
            foreach (Stream unreportedStream in unreportedFavs.GroupBy(stream => stream.Id).Select(grouping => grouping.Last())
                                                    .Favorites()
                                                    .Where(stream => StreamsManager.Streams.Any(stream1 => stream1.Id == stream.Id)))
            {
                new NotificationWindow(unreportedStream);
            }
            unreportedFavs.Clear();
        }

        private static bool IsFullscreenAppRunning()
        {
            var hWnd = NativeMethods.GetForegroundWindow();

            if (hWnd != NativeMethods.GetDesktopWindow() && hWnd != NativeMethods.GetShellWindow())
            {
                NativeMethods.Rect appBounds = new NativeMethods.Rect();
                NativeMethods.GetWindowRect(hWnd, ref appBounds);

                var screen = Screen.FromHandle(hWnd);
                if (screen.GetHashCode() == Screen.PrimaryScreen.GetHashCode())
                {
                    if (screen.Bounds.Bottom == appBounds.bottom && screen.Bounds.Right == appBounds.right)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void IconWindowOnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            StreamListOnClick();
        }

        private void StreamListOnClick(object sender = null, EventArgs eventArgs = null)
        {
            if (streamsWindow == null)
            {
                streamsWindow = new StreamsListWindow(new TimeBlockingDelegate(DuringTimeBlock));
                streamsWindow.Closed += (o, args) => streamsWindow = null;
                streamsWindow.Owner = this;

                streamsWindow.Show();
                streamsWindow.Activate();
            }
            else
            {
                streamsWindow.Activate();
            }
        }

        private void OnTrayExitClick(object sender, EventArgs e)
        {
            App.ExitApp();
        }

        // maybe rework this as a dispatcher timer
        // using a threading timer, not DispatcherTimer, don't access UI directly
        private void mainTimer_Tick(object sender, EventArgs e)
        {
            //            (new NotificationWindow(new Stream("a", "b", 100, "a", "League of Legends", StreamingSite.TwitchTv))).Show();
            try
            {
                DateTime now = DateTime.Now;

                var ownedTask = Task.Factory.StartNew(delegate
                                                          {
                                                              try
                                                              {
                                                                  var ownedResponse = new RestClient("http://api.own3d.tv/live").SinglePageResponse();
                                                                  return XDocument.Parse(ownedResponse.Content);
                                                              }
                                                              catch (Exception exception)
                                                              {
                                                                  Debug.WriteLine(exception);
                                                                  return null;
                                                              }
                                                          }, TaskCreationOptions.PreferFairness
                                                          );

                List<Stream> streamsList = new List<Stream>();
                List<Stream> closedStreams = new List<Stream>();

                // check twitch fav streams through a channel request
                if (ConfigManager.AutoCheckFavorites)
                {
                    try
                    {
                        CheckFavoriteStreamsManually(newStreamsList, streamsList);
                    }
                    catch (Exception) { }
                    closedStreams.AddRange(StreamsManager.Streams.Where(stream => stream.GottenViaAutoGetFavs && !streamsList.Contains(stream)).ToList());
                }

                XDocument xDoc;

                //                                xDoc = XDocument.Load("twitch.xml");
                try
                {
                    var twitchResponse = new RestClient("http://api.justin.tv/api/stream/list.xml?category=gaming&limit=100").SinglePageResponse();
                    xDoc = XDocument.Parse(twitchResponse.Content);
                }
                catch (Exception)
                {
                    xDoc = null;

                    // need to not spam user
                    if (firstRun)
                    {
                        iconWindow.notificationItem.BalloonTip("Trouble reading from TWITCHTV", "REQUEST FAILED", toolTipIcon: ToolTipIcon.Error);
                    }
                }

                //                Debug.WriteLine((DateTime.Now - now).TotalSeconds + " for twitch request");

                if (xDoc != null)
                {
                    IEnumerable<XElement> streams = xDoc.Descendants("stream");

                    foreach (XElement stream in streams)
                    {
                        try
                        {
                            var newStream = LoadTwStreamFromXml(stream);
                            if (!streamsList.Contains(newStream))
                            {
                                if (!StreamsManager.Streams.Contains(newStream))
                                {
                                    Debug.WriteLine("ADDED " + newStream.Name + ", " + newStream.Id);
                                    newStreamsList.Add(newStream);
                                }
                                streamsList.Add(newStream);
                            }
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
                }

                //                XDocument xDocOwned = XDocument.Load("owned.xml");
                var xDocOwned = ownedTask.Result;

                //                Debug.WriteLine((DateTime.Now - now).TotalSeconds + " for owned request");
                if (xDocOwned != null)
                {
                    var ownedStreams = xDocOwned.Descendants("item");
                    foreach (XElement ownedStream in ownedStreams)
                    {
                        try
                        {
                            var newStream = LoadOwnedStreamFromXml(ownedStream);
                            if (!StreamsManager.Streams.Contains(newStream))
                            {
                                Debug.WriteLine("ADDED " + newStream.Name + ", " + newStream.Id);
                                newStreamsList.Add(newStream);
                            }
                            streamsList.Add(newStream);
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
                }
                else
                {
                    // need to not spam user
                    if (firstRun)
                    {
                        iconWindow.notificationItem.BalloonTip("Trouble reading from OWNEDTV", "REQUEST FAILED", toolTipIcon: ToolTipIcon.Error);
                    }
                }

                // check expired streams, without autoGetFavs
                closedStreams.AddRange(StreamsManager.Streams.Where(stream => !stream.GottenViaAutoGetFavs && !streamsList.Contains(stream)).ToList());

                // id of a stream sometimes changes for no reason
                foreach (Stream closedStream in closedStreams.ToList())
                {
                    var newSameStream = newStreamsList.FirstOrDefault(stream => stream.Name == closedStream.Name
                                                                                  && stream.Title == closedStream.Title
                                                                                  && stream.GameName == closedStream.GameName);

                    if (newSameStream != null)
                    {
                        closedStream.Id = newSameStream.Id;
                        closedStreams.Remove(closedStream);
                        newStreamsList.Remove(newSameStream);
                    }
                }

                // update stream data
                foreach (Stream streamOld in StreamsManager.Streams)
                {
                    var streamNew = streamsList.FirstOrDefault(stream => stream.Id == streamOld.Id);
                    if (streamNew != null)
                    {
                        streamOld.UpdateStreamData(streamNew);
                    }
                }

                // add new streams, remove closed ones from the main list
                this.Dispatcher.BeginInvoke((MethodInvoker)(delegate()
                {
                    foreach (Stream closedStream in closedStreams)
                    {
                        StreamsManager.Streams.Remove(closedStream);
                        Debug.WriteLine("REMOVED STREAM " + closedStream.Name);
                    }
                    StreamsManager.Streams.AddRange(newStreamsList);

                    // don't create a new notification for streams readded this pass
                    foreach (Stream stream in newStreamsList.Favorites().Where(stream => closedLastPass.Any(stream1 => stream1.Name == stream.Name)).ToList())
                    {
                        newStreamsList.Remove(stream);
                    }
                    closedLastPass.Clear();
                    closedLastPass.AddRange(closedStreams);

                    if (streamsWindow != null)
                    {
                        streamsWindow.RefreshView();
                    }
                }));

                //first run is in a UI safe thread
                if (firstRun)
                {
                    firstRun = false;

                    if (!DuringTimeBlock())
                    {
                        foreach (Stream newStream in newStreamsList.Favorites())
                        {
                            new NotificationWindow(newStream);
                        }
                    }
                    else
                    {
                        iconWindow.notificationItem.BalloonTip("Currently blocking favorites.", "BLOCKING", toolTipIcon: ToolTipIcon.Info);
                    }

                    if (streamsWindow == null)
                    {
                        StreamListOnClick();
                    }
                }
                else
                {
                    if (IsFullscreenAppRunning())
                    {
                        unreportedFavs.AddRange(newStreamsList);

                        dispatcherTimer.Start();
                    }
                    else
                    {
                        if (!DuringTimeBlock())
                        {
                            var newStreamsListCopy = newStreamsList.ToList();
                            this.Dispatcher.BeginInvoke((MethodInvoker)(delegate()
                                                                            {
                                                                                //                                                                                foreach (Stream newStream in newStreamsListCopy)
                                                                                foreach (Stream newStream in newStreamsList.Favorites())
                                                                                {
                                                                                    new NotificationWindow(newStream);
                                                                                }
                                                                            }));
                        }
                    }
                }

                // useful for debugging
                //                xDoc.Save("twitch.xml");
                //                xDocOwned.Save("owned.xml");
            }
            catch (AggregateException exception)
            {
                if (Debugger.IsAttached) throw;

                foreach (var taskException in exception.InnerExceptions)
                {
                }
            }
            catch (Exception)
            {
                if (Debugger.IsAttached) throw;
            }
            finally
            {
                Debug.WriteLine("".PadRight(45, '-'));
                firstRun = false;
                ConfigManager.UpdatePlotModel(StreamsManager.Streams);

                // need this to be in sync
                this.Dispatcher.BeginInvoke((MethodInvoker)(() => newStreamsList.Clear()));
                timer.Start();
            }
        }

        private bool DuringTimeBlock()
        {
            var timeBlocking = DuringTimeBlockCheck();

            if (!WasTimeBlocking && timeBlocking)
            {
                WasTimeBlocking = true;

                if (streamsWindow != null)
                {
                    //                    this.Dispatcher.BeginInvoke((MethodInvoker)(() => streamsWindow.RefreshView()));
                    streamsWindow.RefreshView();
                }
            }
            else if (WasTimeBlocking && !timeBlocking)
            {
                WasTimeBlocking = false;

                //                this.Dispatcher.BeginInvoke((MethodInvoker)(delegate()
                //                {
                if (streamsWindow != null)
                {
                    streamsWindow.RefreshView();
                }

                var fullscreen = IsFullscreenAppRunning();

                var streamsFromNewPass = StreamsManager.Streams.Favorites().Where(stream => !newStreamsList.Contains(stream)).ToList();

                ParameterizedThreadStart showNotifications = streams =>
                {
                    foreach (Stream newStream in streams as IEnumerable<Stream>)
                    {
                        new NotificationWindow(newStream);
                    }
                };
                if (fullscreen)
                {
                    streamsFromNewPass.ForEach(stream => unreportedFavs.Add(stream));
                }
                else
                {
                    if (Thread.CurrentThread == Dispatcher.CurrentDispatcher.Thread)
                    {
                        showNotifications(streamsFromNewPass);
                    }
                    else
                    {
                        showNotifications(streamsFromNewPass);

                        //                        this.Dispatcher.BeginInvoke((MethodInvoker)showNotifications(streamsFromNewPass));
                    }
                }

                if (fullscreen)
                {
                    dispatcherTimer.Start();
                }

                //                }));
            }

            return timeBlocking;
        }

        public static bool DuringTimeBlockCheck()
        {
            return DateTime.Now.TimeOfDay >= ConfigManager.FromSpan
                       && DateTime.Now.TimeOfDay <= ConfigManager.ToSpan;
        }

        // only checking twitch
        private void CheckFavoriteStreamsManually(List<Stream> newStreamsList, List<Stream> streamsList)
        {
            if (!ConfigManager.FavoriteStreams.Any()) return;

            DateTime now = DateTime.Now;
            string channels = ConfigManager.FavoriteStreams
                .Where(stream => stream.Site == StreamingSite.TwitchTv && streamsList.All(stream1 => stream1.ChannelId != stream.ChannelId))
                .Select(stream => stream.LoginNameTwtv)
                .Aggregate((s, s1) => s + "," + s1);

            var twitchFavsResponse = new RestClient("http://api.justin.tv/api/stream/list.xml?channel=" + channels).SinglePageResponse();
            XDocument xDocument = XDocument.Parse(twitchFavsResponse.Content);

            List<Stream> gottenFavs = new List<Stream>();
            foreach (XElement stream in xDocument.Element("streams").Elements("stream"))
            {
                try
                {
                    gottenFavs.Add(LoadTwStreamFromXml(stream));
                }
                catch (NullReferenceException)
                {
                }
            }

            foreach (Stream stream in gottenFavs)
            {
                stream.GottenViaAutoGetFavs = true;
                if (!StreamsManager.Streams.Contains(stream))
                {
                    Debug.WriteLine("ADDED " + stream.Name + ", " + stream.Id + " FROM FAVORITES CHECK");
                    newStreamsList.Add(stream);
                }
                streamsList.Add(stream);
            }
            Debug.WriteLine("TIMER TO RETRIEVE FAVORITES: " + (DateTime.Now - now).TotalSeconds);
        }

        private Stream LoadTwStreamFromXml(XElement stream)
        {
            string name = null, gameName = "", title = "", id = null, channelId = null, thumbnailURI;
            string twitchLogin = null;
            int viewers = 0;

            name = stream.Element("channel").Element("title").Value;
            viewers = int.Parse(stream.Element("channel_count").Value);
            id = stream.Element("id").Value;
            title = (string)stream.Element("title") ?? "";
            gameName = (string)stream.Element("meta_game") ?? "";
            twitchLogin = stream.Element("channel").Element("login").Value;
            channelId = stream.Element("channel").Element("id").Value;

            //            thumbnailURI = stream.Element("channel").Element("screen_cap_url_large").Value;
            thumbnailURI = stream.Element("channel").Element("screen_cap_url_huge").Value;

            if (gameName == "StarCraft II: Wings of Liberty")
            {
                gameName = "StarCraft II";
            }

            if (name == title)
            {
                title = string.Empty;
            }

            var newStream = new Stream(name, title, viewers, id, channelId, gameName, StreamingSite.TwitchTv) { LoginNameTwtv = twitchLogin, ThumbnailURI = thumbnailURI };
            return newStream;
        }

        private Stream LoadOwnedStreamFromXml(XElement ownedStream)
        {
            string name = null, gameName = null, title = null, id = null, thumbnailURI = null;
            int viewers = 0;

            // channel name worthless in owned
            //                        name = ownedStream.Element("author").Value.Replace("rss@own3d.tv (", "");
            //                        name = name.Remove(name.Length - 1, 1);
            name = ownedStream.Element("title").Value;
            title = name;
            viewers = int.Parse(ownedStream.Element("misc").Attribute("viewers").Value);
            id = ownedStream.Element("guid").Value;
            gameName = ownedStream.Element("misc").Attribute("game").Value;
            thumbnailURI = ownedStream.Element("thumbnail").Value;

            if (gameName == "Diablo 3")
            {
                gameName = "Diablo III";
            }

            if (name == title)
            {
                title = string.Empty;
            }

            var newStream = new Stream(name, title, viewers, id, id, gameName, StreamingSite.OwnedTv) { LoginNameTwtv = name, ThumbnailURI = thumbnailURI };
            return newStream;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            //make invisible to task manager
            int exStyle = (int)NativeMethods.GetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_NOACTIVATE;
            NativeMethods.SetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            HwndSource src = HwndSource.FromHwnd(hwnd);
            src.AddHook(new HwndSourceHook(WndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == App.WM_SHOWFIRSTINSTANCE)
            {
                StreamListOnClick();
            }
            return IntPtr.Zero;
        }
    }
}