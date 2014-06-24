using Caliburn.Micro;
using LeStreamsFace.StreamParsers;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Xml.Linq;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.MessageBox;
using Screen = System.Windows.Forms.Screen;

namespace LeStreamsFace
{
    internal class AppLogic : IHandle<TabCreationEvent>, IHandle<CheckTimeBlockEvent>
    {
        private readonly DispatcherTimer mainTimer;
        private readonly DispatcherTimer fullscreenWaitTimer;
        public static StreamsListWindow streamsWindow;
        public static bool WasTimeBlocking = false;
        private bool firstRun = true;
        private readonly object _syncLock = new object();

        public static readonly Taskbar Taskbar = new Taskbar();
        public static readonly List<NotificationWindow> NotificationWindows = new List<NotificationWindow>();

        private readonly IconWindow iconWindow;

        private readonly List<Stream> newStreamsList = new List<Stream>();
        private readonly List<Stream> unreportedFavs = new List<Stream>();
        private readonly List<Stream> closedLastPass = new List<Stream>();

        public delegate void ExitDelegate(object sender, EventArgs e);

        private readonly IStreamParser<XElement> _streamParserXML;
        private readonly IStreamParser<JToken> _streamParserJSON;

        public static IEventAggregator EventAggregator;

        public AppLogic(IStreamParser<XElement> streamParserXML, IStreamParser<JToken> streamParserJSON)
        {
            _streamParserXML = streamParserXML;
            _streamParserJSON = streamParserJSON;
#if DEBUG
            var traceListener = new TextWriterTraceListener(System.IO.File.CreateText("Trace.txt"));
            Debug.Listeners.Add(traceListener);
            Debug.AutoFlush = true;
#endif

            EventAggregator = new EventAggregator();
            EventAggregator.Subscribe(this);

            try
            {
                ConfigManager.Instance.ReadConfigXml();

                ExitDelegate exitDelegate = new ExitDelegate(OnTrayExitClick);

                iconWindow = new IconWindow(exitDelegate);

                List<MenuItem> menuItems = new List<MenuItem>(iconWindow.notificationItem.MenuItems);
                menuItems.Insert(0, new MenuItem("-"));

                iconWindow.notificationItem.TrayContextMenu = new ContextMenu(menuItems.ToArray());
                iconWindow.notificationItem.TrayContextMenu.MenuItems.Add(0, new MenuItem("Stream list", StreamListOnClick));

                iconWindow.MouseLeftButtonDown += StreamListOnClick;
                iconWindow.Show();

                fullscreenWaitTimer = new DispatcherTimer();
                fullscreenWaitTimer.Interval = new TimeSpan(0, 0, 0, 0, 2500);
                fullscreenWaitTimer.Tick += FullscreenWait;

                mainTimer = new DispatcherTimer();
                mainTimer.Interval = new TimeSpan(0, 0, 0, ConfigManager.Instance.SamplingInterval);
                mainTimer.Tick += MainTimerTick;

                MainTimerTick();
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
                fullscreenWaitTimer.Stop();
                return;
            }

            if (IsFullscreenAppRunning())
            {
                return;
            }

            fullscreenWaitTimer.Stop();

            // remove duplicates in unreportedFavs, and display currently existing favorites
            foreach (Stream unreportedStream in unreportedFavs.GroupBy(stream => stream.Id)
                                                    .Select(grouping => grouping.Last())
                                                    .Favorites()
                                                    .Where(stream => StreamsManager.Streams.Any(stream1 => stream1 == stream)))
            {
                new NotificationWindow(unreportedStream, EventAggregator);
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

        private void StreamListOnClick(object sender = null, EventArgs eventArgs = null)
        {
            if (streamsWindow == null)
            {
                CreateStreamsListWindow();
            }

            streamsWindow.Activate();

            // make sure we start topmost
            streamsWindow.Topmost = true;
            streamsWindow.Topmost = false;
            streamsWindow.Focus();
        }

        private void CreateStreamsListWindow()
        {
            lock (_syncLock)
            {
                if (streamsWindow == null)
                {
                    streamsWindow = new StreamsListWindow(EventAggregator);
                    streamsWindow.Closing += StreamsWindowOnClosing;
                    streamsWindow.Show();
                    StreamsListWindow.DoneWithFirstOpening = true;
                }
            }
        }

        private void StreamsWindowOnClosing(object sender, CancelEventArgs cancelEventArgs)
        {
            streamsWindow.Closing -= StreamsWindowOnClosing;
            streamsWindow = null;
        }

        private void OnTrayExitClick(object sender, EventArgs e)
        {
            App.ExitApp();
        }

        private async void MainTimerTick(object sender = null, EventArgs e = null)
        {
            mainTimer.Stop();

            if (firstRun && streamsWindow == null)
            {
                StreamListOnClick();
            }

            try
            {
                //                var twitchTask = Task.Factory.StartNew( () =>
                //                                                        {
                //                                                            var twitchResponse = new RestClient("https://api.twitch.tv/kraken/streams?limit=100").SinglePageResponse();
                //                                                            return _streamParserJSON.GetStreamsFromContent(twitchResponse.Content);
                //                                                        }, TaskCreationOptions.PreferFairness);

                var streamsList = new List<Stream>();
                var closedStreams = new List<Stream>();

                // check twitch fav streams through a channel request
                if (ConfigManager.Instance.AutoCheckFavorites)
                {
                    try
                    {
                        await CheckFavoriteTwitchStreamsManually(newStreamsList, streamsList);
                    }
                    catch (Exception) { }
                    closedStreams.AddRange(StreamsManager.Streams.Where(stream => stream.GottenViaAutoGetFavs && !streamsList.Contains(stream)).ToList());
                }

                IEnumerable<Stream> twitchFetchedStreams = Enumerable.Empty<Stream>();
                try
                {
                    var twitchResponse = await new RestClient("https://api.twitch.tv/kraken/streams?limit=100").ExecuteTaskAsync(new RestRequest());
                    twitchFetchedStreams = _streamParserJSON.GetStreamsFromContent(twitchResponse.Content);
                }
                catch (Exception exception)
                {
                    iconWindow.notificationItem.BalloonTip("Trouble reading from TWITCHTV", "REQUEST FAILED", toolTipIcon: ToolTipIcon.Error);
                    Debug.WriteLine(exception);
                }

                // add streams we didn't get from favs to streamsList, new streams to newStreamsList
                foreach (Stream stream in twitchFetchedStreams)
                {
                    if (!streamsList.Contains(stream))
                    {
                        if (!StreamsManager.Streams.Contains(stream))
                        {
                            //                            Debug.WriteLine("ADDED " + stream.Name + ", " + stream.Id);
                            newStreamsList.Add(stream);
                        }
                        streamsList.Add(stream);
                    }
                }

                // check expired streams, without autoGetFavs
                closedStreams.AddRange(StreamsManager.Streams.Where(stream => !stream.GottenViaAutoGetFavs && !streamsList.Contains(stream)));

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
                StreamsManager.Streams.RemoveRange(closedStreams);
                foreach (Stream closedStream in closedStreams)
                {
                    var existingNotification = NotificationWindows.FirstOrDefault(window => ReferenceEquals(window.DataContext, closedStream));
                    if (existingNotification != null)
                    {
                        existingNotification.Close();
                    }
                    //                    Debug.WriteLine("REMOVED STREAM " + closedStream.Name);
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

                if (firstRun)
                {
                    if (!DuringTimeBlock())
                    {
                        foreach (Stream newStream in newStreamsList.Favorites())
                        {
                            new NotificationWindow(newStream, EventAggregator);
                        }
                    }
                    else
                    {
                        iconWindow.notificationItem.BalloonTip("Currently blocking favorites.", "BLOCKING", toolTipIcon: ToolTipIcon.Info);
                    }
                }
                else
                {
                    if (!ConfigManager.Instance.Offline)
                    {
                        if (IsFullscreenAppRunning())
                        {
                            unreportedFavs.AddRange(newStreamsList);

                            fullscreenWaitTimer.Start();
                        }
                        else
                        {
                            if (!DuringTimeBlock())
                            {
                                // foreach (Stream newStream in newStreamsList)
                                foreach (Stream newStream in newStreamsList.Favorites())
                                {
                                    new NotificationWindow(newStream, EventAggregator);
                                }
                            }
                        }
                    }
                }
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
                //                Debug.WriteLine("".PadRight(45, '-'));
                firstRun = false;
                ConfigManager.Instance.Offline = !StreamsManager.Streams.Any();
                if (ConfigManager.Instance.Offline)
                {
                    unreportedFavs.Clear();
                }
                ConfigManager.Instance.UpdatePlotModel(StreamsManager.Streams);

                newStreamsList.Clear();
                mainTimer.Start();
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
                    streamsWindow.RefreshView();
                }
            }
            else if (WasTimeBlocking && !timeBlocking)
            {
                WasTimeBlocking = false;

                if (streamsWindow != null)
                {
                    streamsWindow.RefreshView();
                }

                var fullscreen = IsFullscreenAppRunning();
                var streamsFromOldPasses = StreamsManager.Streams.Favorites().Where(stream => !newStreamsList.Contains(stream)).ToList();

                if (fullscreen)
                {
                    unreportedFavs.AddRange(streamsFromOldPasses);
                    fullscreenWaitTimer.Start();
                }
                else
                {
                    foreach (Stream newStream in streamsFromOldPasses)
                    {
                        new NotificationWindow(newStream, EventAggregator);
                    }
                }
            }

            return timeBlocking;
        }

        private static bool DuringTimeBlockCheck()
        {
            // e.g. 0600-2200
            if (ConfigManager.Instance.FromSpan <= ConfigManager.Instance.ToSpan)
            {
                return DateTime.Now.TimeOfDay >= ConfigManager.Instance.FromSpan
                       && DateTime.Now.TimeOfDay <= ConfigManager.Instance.ToSpan;
            }

            // e.g. 2200-0600
            return DateTime.Now.TimeOfDay >= ConfigManager.Instance.FromSpan
                   || DateTime.Now.TimeOfDay <= ConfigManager.Instance.ToSpan;
        }

        private async Task CheckFavoriteTwitchStreamsManually(List<Stream> newStreamsList, List<Stream> streamsList)
        {
            if (!ConfigManager.Instance.FavoriteStreams.Any()) return;

            DateTime now = DateTime.Now;
            string channels = ConfigManager.Instance.FavoriteStreams
                .Where(stream => stream.Site == StreamingSite.TwitchTv && streamsList.All(stream1 => stream1.ChannelId != stream.ChannelId))
                .Select(stream => stream.LoginNameTwtv)
                .Aggregate((s, s1) => s + "," + s1);

            var twitchFavsResponse = await new RestClient("http://api.justin.tv/api/stream/list.xml?channel=" + channels).ExecuteTaskAsync(new RestRequest());
            XDocument xDocument = XDocument.Parse(twitchFavsResponse.Content);

            var gottenFavs = new List<Stream>();
            foreach (XElement xElement in xDocument.Element("streams").Elements("stream"))
            {
                try
                {
                    gottenFavs.Add(_streamParserXML.GetStreamFromElement(xElement));
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
            //            Debug.WriteLine("TIMER TO RETRIEVE FAVORITES: " + (DateTime.Now - now).TotalSeconds);
        }

        //        private void Window_SourceInitialized(object sender, EventArgs e)
        //        {
        //            var hwnd = new WindowInteropHelper(this).Handle;
        //
        //            // make invisible to task manager
        //            int exStyle = (int)NativeMethods.GetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE);
        //            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
        //            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_NOACTIVATE;
        //            NativeMethods.SetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        //
        //            HwndSource src = HwndSource.FromHwnd(hwnd);
        //            src.AddHook(new HwndSourceHook(WndProc));
        //        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == App.WM_SHOWFIRSTINSTANCE)
            {
                StreamListOnClick();
            }
            return IntPtr.Zero;
        }

        public void Handle(TabCreationEvent message)
        {
            if (ConfigManager.Instance.StreamOpeningProcedure == StreamOpeningProcedure.Livestreamer)
            {
                CreateLivestreamerConsole(message.Stream);
            }
            if (ConfigManager.Instance.StreamOpeningProcedure == StreamOpeningProcedure.Tab)
            {
                if (streamsWindow == null)
                {
                    CreateStreamsListWindow();
                    EventAggregator.PublishOnCurrentThread(message);
                }
            }
            if (ConfigManager.Instance.StreamOpeningProcedure == StreamOpeningProcedure.Browser)
            {
                Process.Start(message.Stream.GetUrl());
            }
        }

        private void CreateLivestreamerConsole(Stream streamToStart)
        {
            //            var args = streamToStart.GetUrl() + " " + ConfigManager.Instance.LivestreamerArguments;
            var args = "twitch.tv/" + streamToStart.LoginNameTwtv + " " + ConfigManager.Instance.LivestreamerArguments;
            var processStartInfo = new ProcessStartInfo(@"livestreamer\livestreamer.exe", args);

            processStartInfo.UseShellExecute = false;
            processStartInfo.ErrorDialog = false;

            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.CreateNoWindow = true;

            Task.Factory.StartNew(() =>
                                  {
                                      Process process = new Process();
                                      process.StartInfo = processStartInfo;
                                      bool processStarted = process.Start();

                                      StreamWriter inputWriter = process.StandardInput;
                                      StreamReader outputReader = process.StandardOutput;
                                      StreamReader errorReader = process.StandardError;
                                      var consoleOutput = outputReader.ReadToEnd();
                                      process.WaitForExit();
                                  });
        }

        public void Handle(CheckTimeBlockEvent message)
        {
            DuringTimeBlock();
        }
    }
}