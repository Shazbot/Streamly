using LeStreamsFace.Annotations;
using LeStreamsFace.StreamParsers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LeStreamsFace
{
    [ImplementPropertyChanged]
    internal class StreamsListViewModel : INotifyPropertyChanged
    {
        public readonly StreamsListWindow View;

        private GamesViewModel _gamesPanelSelectedGame;
        private Stream _streamsPanelSelectedStream;

        public StreamsListViewModel(StreamsListWindow view)
        {
            this.View = view;

            _runningStreams.CollectionChanged += RunningStreamsOnCollectionChanged;

            GamesPanelToggleCommand = new DelegateCommand(ToggleGamesPanel);
            StreamingTabClickedCommand = new DelegateCommand<Stream>(stream => OpenExistingStreamingTab(stream));
            GetTwitchFavoritesCommand = new DelegateCommand<string>(usernameOnTwitch => ImportTwitchFavorites(usernameOnTwitch));
            RefreshViewCommand = new DelegateCommand(() => view.RefreshView());
            UnfavoriteStreamCommand = new DelegateCommand<FavoriteStream>(param => UnfavoriteStream(param));
            FavoriteAStreamCommand = new DelegateCommand<Stream>(stream => FavoriteAStream(stream));
            ChangeShellTabCommand = new DelegateCommand<TabItem>(tab => ChangeShellTab(tab));
            CloseStreamingTabCommand = new DelegateCommand(() => CloseStreamingTab());

            ShellContainerMargin = new Thickness(0);

            FetchGames();

            // TODO

            var str = new Stream("wingsofdeathx", "wings", 123, "1", "ID1", "asr", StreamingSite.TwitchTv);
            var str2 = new Stream("ongamenet", "wings", 123, "2", "ID2", "asr", StreamingSite.TwitchTv);
            //TODO if we want to start with a stream
            str.LoginNameTwtv = "wingsofdeath";

            str.LoginNameTwtv = "riotgames";
            str2.LoginNameTwtv = "ongamenet";

            RunningStreams.Add(str);
            RunningStreams.Add(str2);
            IsAnyStreamTabOpen = true;
            SelectedRunningStreamTab = str;
            // TODO maybe move this to switching tab logic
            // TODO can we use the property itself or it's ok like this?
        }

        private void CloseStreamingTab()
        {
            var streamToRemove = SelectedRunningStreamTab;
            SelectedRunningStreamTab = RunningStreams.FirstOrDefault(stream => stream != SelectedRunningStreamTab);

            RunningStreams.Remove(streamToRemove);

            if (SelectedRunningStreamTab == null)
            {
                IsAnyStreamTabOpen = false;
            }
            OnPropertyChanged(Extensions.GetVariableName(() => CloseStreamsButtonVisibility));
        }

        private void OpenExistingStreamingTab(Stream stream)
        {
            CloseFlyouts();

            SelectedRunningStreamTab = stream;
            IsAnyStreamTabOpen = true;
        }

        private void ChangeShellTab(TabItem tab)
        {
            CloseFlyouts();
            IsAnyStreamTabOpen = false;

            if (tab == View.streamsTabItem)
            {
                if (tab.IsSelected)
                {
                    GameFilteringByIconsEnabled = !GameFilteringByIconsEnabled;
                    View.RefreshView();
                }
            }
            else if (tab == View.configTabItem)
            {
                TimeWhenNotNotifyingTextInput = ConfigManager.Instance.FromSpan.ToString("hhmm") + '-' + ConfigManager.Instance.ToSpan.ToString("hhmm");
                BannedGamesTextInput = ConfigManager.Instance.BannedGames.Aggregate((s, s1) => s + ", " + s1);
            }
            tab.IsSelected = true;
        }

        private void FavoriteAStream(Stream stream)
        {
            stream.IsFavorite = !stream.IsFavorite;

            View.RefreshView();

            if (!stream.IsFavorite)
            {
                ConfigManager.Instance.FavoriteStreams.Remove(ConfigManager.Instance.FavoriteStreams.SingleOrDefault(stream1 => stream1.ChannelId == stream.ChannelId));
            }
            else
            {
                if (ConfigManager.Instance.FavoriteStreams.FromSite(stream.Site).NoStreamWithChannelId(stream.ChannelId))
                {
                    ConfigManager.Instance.FavoriteStreams.Add(new FavoriteStream(stream.LoginNameTwtv, stream.ChannelId, stream.Site));
                }
            }
            ConfigManager.Instance.WriteConfigXml();
        }

        public ICommand RefreshViewCommand { get; private set; }

        public ICommand GetTwitchFavoritesCommand { get; private set; }

        public ICommand UnfavoriteStreamCommand { get; private set; }

        private void ImportTwitchFavorites(string usernameOnTwitch)
        {
            if (string.IsNullOrWhiteSpace(usernameOnTwitch))
            {
                return;
            }

            IEnumerable<Stream> favoritesFromTwitch;
            try
            {
                var client = new RestClient("https://api.twitch.tv/kraken/users/" + usernameOnTwitch + "/follows/channels?limit=100");
                var request = new RestRequest();
                client.AddHandler("application/json", new RestSharpJsonNetSerializer());
                var resp = client.Execute<JObject>(request);
                if (resp.Data["error"] != null)
                {
                    throw new WebException();
                }
                favoritesFromTwitch = resp.Data["follows"].Select(token => (Stream)token["channel"].ToObject(typeof(Stream)));
            }
            catch (WebException)
            {
                MessageBox.Show("Username doesn't exist (404)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var needToWriteConfig = false;
            foreach (var favoriteChannel in favoritesFromTwitch)
            {
                if (ConfigManager.Instance.FavoriteStreams.Where(stream => stream.Site == StreamingSite.TwitchTv).All(stream => favoriteChannel.Id != stream.ChannelId))
                {
                    ConfigManager.Instance.FavoriteStreams.Add(new FavoriteStream(favoriteChannel.Name, favoriteChannel.Id, StreamingSite.TwitchTv));
                    StreamsManager.Streams.Where(stream => stream.Site == StreamingSite.TwitchTv && stream.ChannelId == favoriteChannel.Id).ToList().ForEach(stream => stream.IsFavorite = true);
                    needToWriteConfig = true;
                }
            }

            if (needToWriteConfig)
            {
                MessageBox.Show("Successfully imported favorites.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ConfigManager.Instance.WriteConfigXml();
                View.RefreshView();
            }
            else
            {
                MessageBox.Show("No new streams to import.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UnfavoriteStream(FavoriteStream streamToUnfavorite)
        {
            ConfigManager.Instance.FavoriteStreams.Remove(streamToUnfavorite);

            var ourFavorites = StreamsManager.Streams.Where(stream => stream.ChannelId == streamToUnfavorite.ChannelId);
            if (ourFavorites.Any())
            {
                foreach (var ourFavorite in ourFavorites)
                {
                    ourFavorite.IsFavorite = false;
                }
                View.RefreshView();
                ConfigManager.Instance.WriteConfigXml();
            }
        }

        private async Task FetchGames()
        {
            // TODO make this rerun after a timeout
            //                        var twitchResponse = await new RestClient("https://api.twitch.tv/kraken/games/top?limit=100").ExecuteTaskAsync(new RestRequest(),);
            try
            {
                var twitchResponse = new RestClient("https://api.twitch.tv/kraken/games/top?limit=100").Execute(new RestRequest());
                var topGamesJObject = JsonConvert.DeserializeObject<JObject>(twitchResponse.Content)["top"];
                var games = topGamesJObject.Children().Select(token => new GamesViewModel(token["game"]["name"].ToString(), token["game"]["box"]["medium"].ToString()));
                // sometimes we get no Games (Games.Count == 0)
                var gc = games.Count();
                Games.AddRange(games);

                if (Games.Count == 0) // examine response
                {
                    Debugger.Break();
                }
            }
            catch (Exception e)
            {
            }
        }

        private void ToggleGamesPanel()
        {
            if (IsGamesPanelOpen && IsStreamsPanelOpen)
            {
                IsStreamsPanelOpen = false;
            }
            else
            {
                IsGamesPanelOpen = !IsGamesPanelOpen;
            }
        }

        private void RunningStreamsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
        }

        #region RunningStreams

        private readonly OptimizedObservableCollection<Stream> _runningStreams = new OptimizedObservableCollection<Stream>();

        public OptimizedObservableCollection<Stream> RunningStreams
        {
            get { return _runningStreams; }
        }

        #endregion RunningStreams

        #region Games

        private readonly OptimizedObservableCollection<GamesViewModel> _games = new OptimizedObservableCollection<GamesViewModel>();

        public OptimizedObservableCollection<GamesViewModel> Games
        {
            get { return _games; }
        }

        #endregion Games

        #region Streams

        private readonly OptimizedObservableCollection<Stream> _streams = new OptimizedObservableCollection<Stream>();
        private Stream _selectedRunningStreamTab;
        private string _bannedGamesTextInput;
        private string _timeWhenNotNotifyingTextInput;
        private Thickness _shellContainerMargin;

        public OptimizedObservableCollection<Stream> Streams
        {
            get { return _streams; }
        }

        #endregion Streams

        public GamesViewModel GamesPanelSelectedGame
        {
            get { return _gamesPanelSelectedGame; }
            set
            {
                if (value == null && _gamesPanelSelectedGame != null) // value is null if we select the same game in the games panel
                {
                    IsStreamsPanelOpen = true;
                    FetchStreams(_gamesPanelSelectedGame.GameName); // refresh the stream list
                }
                else if (value != null) // selected a new game to show streams for
                {
                    _gamesPanelSelectedGame = value;
                    FetchStreams(value.GameName);
                }
            }
        }

        private async void FetchStreams(string gameName)
        {
            var twitchResponse = await new RestClient("https://api.twitch.tv/kraken/search/streams?limit=20&q=" + gameName).ExecuteTaskAsync(new RestRequest());
            var streams = (new TwitchJSONStreamParser()).GetStreamsFromContent(twitchResponse.Content);
            Streams.RemoveAll();
            Streams.AddRange(streams);
        }

        public Stream StreamsPanelSelectedStream
        {
            get { return _streamsPanelSelectedStream; }
            set
            {
                if (value == null) return;
                _streamsPanelSelectedStream = value;
                OpenNewStreamingTab(value);
            }
        }

        private void OpenNewStreamingTab(Stream streamToStream)
        {
            //            View.streamsPanel.IsEnabled = false; // if we close the panel and the LMB is down we will select and start multiple streams
            RunningStreams.Add(streamToStream);
            CloseFlyouts();
            IsAnyStreamTabOpen = true;
            OnPropertyChanged(Extensions.GetVariableName(() => CloseStreamsButtonVisibility));

            SelectedRunningStreamTab = streamToStream;
        }

        private void CloseFlyouts()
        {
            IsStreamsPanelOpen = false;
            IsGamesPanelOpen = false;
        }

        public bool CloseStreamsButtonVisibility
        {
            get { return RunningStreams.Count != 0; }
        }

        public bool IsGamesPanelOpen { get; set; }

        public bool IsStreamsPanelOpen
        {
            get { return _isStreamsPanelOpen; }
            set
            {
                _isStreamsPanelOpen = value;
                if (!value)
                {
                    Streams.RemoveAll();
                }
            }
        }

        public ICommand GamesPanelToggleCommand { get; private set; }

        public ICommand StreamingTabClickedCommand { get; private set; }

        public bool IsAnyStreamTabOpen { get; set; }

        public string BannedGamesTextInput
        {
            get { return _bannedGamesTextInput; }
            set
            {
                if (value == _bannedGamesTextInput) return;
                _bannedGamesTextInput = value;
                FilterBannedGames(value);
            }
        }

        private void FilterBannedGames(string bannedGamesInputText)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(bannedGamesInputText))
                {
                    var bannedGames = bannedGamesInputText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
                    ConfigManager.Instance.BannedGames = bannedGames.ToList();
                }
                else
                {
                    ConfigManager.Instance.BannedGames.Clear();
                }
                ConfigManager.Instance.WriteConfigXml();
                View.RefreshView();
            }
            catch (Exception)
            {
            }
        }

        public string TimeWhenNotNotifyingTextInput
        {
            get { return _timeWhenNotNotifyingTextInput; }
            set
            {
                if (value == _timeWhenNotNotifyingTextInput) return;
                _timeWhenNotNotifyingTextInput = value;
                DisableNotificationsDuringThisTime(value);
            }
        }

        private void DisableNotificationsDuringThisTime(string timeInputText)
        {
            try
            {
                TimeSpan fromSpan = new TimeSpan(0, 0, 0);
                TimeSpan toSpan = new TimeSpan(0, 0, 0);

                if (!string.IsNullOrWhiteSpace(timeInputText))
                {
                    string from = timeInputText.Split('-')[0];
                    string to = timeInputText.Split('-')[1];

                    fromSpan = new TimeSpan(int.Parse(from.Substring(0, 2)), int.Parse(from.Substring(2, 2)), 0);
                    toSpan = new TimeSpan(int.Parse(to.Substring(0, 2)), int.Parse(to.Substring(2, 2)), 0);

                    if (fromSpan.TotalSeconds != 0 && toSpan.TotalSeconds != 0)
                    {
                        if (fromSpan.CompareTo(toSpan) == 0)
                        {
                            return;
                        }
                    }
                }
                ConfigManager.Instance.FromSpan = fromSpan;
                ConfigManager.Instance.ToSpan = toSpan;
                ConfigManager.Instance.WriteConfigXml();
                // TODO think about if we need this check here
                //                timeBlockCheck();
            }
            catch (Exception)
            {
            }
        }

        public string GameFilteringTextInput { get; set; }

        public bool GameFilteringByIconsEnabled = true;
        private bool _isStreamsPanelOpen;

        public bool Filter(object o)
        {
            Stream stream = (Stream)o;

            if (stream.IsFavorite && MainWindow.WasTimeBlocking)
            {
                return false;
            }

            var searchText = GameFilteringTextInput;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (stream.Name.ToLower().Contains(searchText.ToLower())
                        || stream.Title.ToLower().Contains(searchText.ToLower())
                        || stream.GameName.ToLower().Contains(searchText.ToLower()))
                {
                    return true;
                }
                return false;
            }

            if (ConfigManager.Instance.BannedGames.Any(stream.GameName.ContainsIgnoreCase))
            {
                return false;
            }

            if (stream.Viewers < ConfigManager.Instance.TriageStreams && !stream.IsFavorite)
            {
                return false;
            }

            if (GameFilteringByIconsEnabled)
            {
                if (StreamsManager.Filters.Any(pair => pair.Value ?? false))
                {
                    foreach (
                        KeyValuePair<FiltersEnum, bool?> keyValuePair in
                            StreamsManager.Filters.Where(pair => pair.Value == true))
                    {
                        var description =
                            ((DescriptionAttribute)
                             typeof(FiltersEnum).GetMember(keyValuePair.Key.ToString())[0].GetCustomAttributes(
                                 typeof(DescriptionAttribute), false)[0]).Description;
                        if (stream.GameName == description)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                foreach (
                    KeyValuePair<FiltersEnum, bool?> keyValuePair in
                        StreamsManager.Filters.Where(pair => pair.Value == false))
                {
                    var description =
                        ((DescriptionAttribute)
                         typeof(FiltersEnum).GetMember(keyValuePair.Key.ToString())[0].GetCustomAttributes(
                             typeof(DescriptionAttribute), false)[0]).Description;
                    if (stream.GameName == description)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public Stream SelectedRunningStreamTab
        {
            get { return _selectedRunningStreamTab; }
            set
            {
                if (value == _selectedRunningStreamTab) return;
                _selectedRunningStreamTab = value;
            }
        }

        public TabItem SelectedShellTab { get; set; }

        public bool IsConfigTabSelected { get; set; }

        public ICommand FavoriteAStreamCommand { get; private set; }

        public ICommand ChangeShellTabCommand { get; private set; }

        public Thickness ShellContainerMargin
        {
            get { return _shellContainerMargin; }
            set
            {
                _shellContainerMargin = value;
            }
        }

        public ICommand CloseStreamingTabCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void StartStream(Stream streamToStart)
        {
            switch (ConfigManager.Instance.StreamOpeningProcedure)
            {
                case StreamOpeningProcedure.Browser:
                    Process.Start(streamToStart.GetUrl());
                    break;

                case StreamOpeningProcedure.Tab:
                    OpenNewStreamingTab(streamToStart);
                    break;

                case StreamOpeningProcedure.Livestreamer:
                    CreateLivestreamerConsole(streamToStart);
                    break;
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
    }
}