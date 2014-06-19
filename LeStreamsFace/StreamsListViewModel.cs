using System.Windows.Controls;
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
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace LeStreamsFace
{
    [ImplementPropertyChanged]
    internal class StreamsListViewModel : INotifyPropertyChanged
    {
        private StreamsListWindow view;

        private GamesViewModel _gamesPanelSelectedGame;
        private Stream _streamsPanelSelectedStream;

        public StreamsListViewModel(StreamsListWindow view)
        {
            this.view = view;

            _runningStreams.CollectionChanged += RunningStreamsOnCollectionChanged;

            GamesPanelButtonPressedCommand = new DelegateCommand(ToggleGamesPanel);
            StreamingTabClickedCommand = new DelegateCommand(() => OpenExistingStreamingTab());
            GetTwitchFavoritesCommand = new DelegateCommand<string>(usernameOnTwitch => ImportTwitchFavorites(usernameOnTwitch));
            RefreshViewCommand = new DelegateCommand(() => view.RefreshView());
            UnfavoriteStreamCommand = new DelegateCommand<FavoriteStream>(param => UnfavoriteStream(param));
            FavoriteAStreamCommand = new DelegateCommand<Stream>(stream => FavoriteAStream(stream));
            ChangeShellTabCommand = new DelegateCommand<TabItem>(tab => ChangeShellTab(tab));

	    ShellContainerMargin = new Thickness(0);

            FetchGames();

            // TODO

            var str = new Stream("wingsofdeathx", "wings", 123, "123", "ra", "asr", StreamingSite.TwitchTv);
            str.LoginNameTwtv = "wingsofdeath";
            RunningStreams.Add(str);
            RunningStreams.Add(new Stream("wingsofdeathx", "wings", 123, "123", "ra", "asr", StreamingSite.TwitchTv));
            RunningStreams.Add(new Stream("wingsofdeathx", "wings", 123, "123", "ra", "asr", StreamingSite.TwitchTv));

            //TODO if we want to start with a stream
            //            IsAnyStreamTabOpen = true;
            //            SelectedStreamTab = str;
            // TODO maybe move this to switching tab logic
            // TODO can we use the property itself or it's ok like this?

        }

        private void OpenExistingStreamingTab()
        {
            IsAnyStreamTabOpen = true;
        }

        private void ChangeShellTab(TabItem tab)
        {
            IsGamesPanelOpen = false;
            IsStreamsPanelOpen = false;

            if (tab == view.streamsTabItem)
            {
                if (tab.IsSelected)
                {
//                    doFilterGames = !doFilterGames;

                    view.RefreshView();
                }
            }
            else if (tab == view.configTabItem)
            {
		            TimeWhenNotNotifyingTextInput = ConfigManager.Instance.FromSpan.ToString("hhmm") + '-' + ConfigManager.Instance.ToSpan.ToString("hhmm");
            BannedGamesTextInput = ConfigManager.Instance.BannedGames.Aggregate((s, s1) => s + ", " + s1);
            }
            tab.IsSelected = true;
        }

        private void FavoriteAStream(Stream stream)
        {
            stream.IsFavorite = !stream.IsFavorite;

            view.RefreshView();

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
                view.RefreshView();
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
                view.RefreshView();
                ConfigManager.Instance.WriteConfigXml();
            }
        }

        private async void FetchGames()
        {
            var twitchResponse = await new RestClient("https://api.twitch.tv/kraken/games/top?limit=100").ExecuteTaskAsync(new RestRequest());
            var topGamesJObject = JsonConvert.DeserializeObject<JObject>(twitchResponse.Content)["top"];
            var games = topGamesJObject.Children().Select(token => new GamesViewModel(token["game"]["name"].ToString(), token["game"]["box"]["medium"].ToString()));
            Games.AddRange(games);
        }

        private void ToggleGamesPanel()
        {
            IsGamesPanelOpen = !IsGamesPanelOpen;
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
        private Stream _selectedStreamTab;
        private string _bannedGamesTextInput;
        private string _timeWhenNotNotifyingTextInput;
        private TabItem _selectedShellTab;
        private bool _isConfigTabSelected;
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
                //                if (value == _gamesPanelSelectedGame) return;
                _gamesPanelSelectedGame = value;
                FetchStreams(_gamesPanelSelectedGame.GameName);
                IsStreamsPanelOpen = true;
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
                _streamsPanelSelectedStream = value;
                OpenNewStreamingTab(value);
            }
        }

        private void OpenNewStreamingTab(Stream streamToStream)
        {
            RunningStreams.Add(streamToStream);
            IsStreamsPanelOpen = false;
            IsGamesPanelOpen = false;
            IsAnyStreamTabOpen = true;
            OnPropertyChanged(Extensions.GetVariableName(() => CloseStreamsButtonVisibility));

            SelectedStreamTab = streamToStream;
        }

        public bool CloseStreamsButtonVisibility
        {
            get { return RunningStreams.Count != 0; }
        }

        public bool IsGamesPanelOpen { get; set; }

        public bool IsStreamsPanelOpen { get; set; }

        public ICommand GamesPanelButtonPressedCommand { get; private set; }

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
                view.RefreshView();
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

        public Stream SelectedStreamTab
        {
            get { return _selectedStreamTab; }
            set
            {
                if (value == _selectedStreamTab) return;
                _selectedStreamTab = value;
            }
        }

        public TabItem SelectedShellTab
        {
            get { return _selectedShellTab; }
            set
            {
                _selectedShellTab = value;
            }
        }

        public bool IsConfigTabSelected
        {
            get { return _isConfigTabSelected; }
            set
            {
                _isConfigTabSelected = value;
            }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}