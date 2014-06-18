using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using LeStreamsFace.Annotations;
using LeStreamsFace.StreamParsers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using RestSharp;

namespace LeStreamsFace
{
    [ImplementPropertyChanged]
    class StreamsListViewModel : INotifyPropertyChanged
    {
        private StreamsListWindow _streamsListWindow;

        private GamesViewModel _gamesPanelSelectedGame;
        private Stream _streamsPanelSelectedStream;

        public StreamsListViewModel(StreamsListWindow streamsListWindow)
        {
            _streamsListWindow = streamsListWindow;

            _runningStreams.CollectionChanged += RunningStreamsOnCollectionChanged;

            GamesPanelButtonPressed = new DelegateCommand(ToggleGamesPanel);
            StreamingTabClicked = new DelegateCommand(() => IsAnyStreamTabOpen = true);

            FetchGames();

            // TODO

            var str = new Stream("wingsofdeathx","wings",123,"123","ra","asr",StreamingSite.TwitchTv);
            RunningStreams.Add(str);
            RunningStreams.Add(new Stream("wingsofdeathx","wings",123,"123","ra","asr",StreamingSite.TwitchTv));
            RunningStreams.Add(new Stream("wingsofdeathx","wings",123,"123","ra","asr",StreamingSite.TwitchTv));
            IsAnyStreamTabOpen = true;
            SelectedStreamTab = str;
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

        #endregion

        #region Games

        private readonly OptimizedObservableCollection<GamesViewModel> _games = new OptimizedObservableCollection<GamesViewModel>();

        public OptimizedObservableCollection<GamesViewModel> Games
        {
            get { return _games; }
        }

        #endregion

        #region Streams

        private readonly OptimizedObservableCollection<Stream> _streams = new OptimizedObservableCollection<Stream>();
        private Stream _selectedStreamTab;

        public OptimizedObservableCollection<Stream> Streams
        {
            get { return _streams; }
        }

        #endregion

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
            OnPropertyChanged(Extensions.GetVariableName(() => CloseStreamsButtonVisibility));
        }

        public bool CloseStreamsButtonVisibility
        {
            get { return RunningStreams.Count != 0; }
        }

        public bool IsGamesPanelOpen { get; set; }

        public bool IsStreamsPanelOpen { get; set; }

        public ICommand GamesPanelButtonPressed { get; private set; }
        public ICommand StreamingTabClicked { get; private set; }

        public bool IsAnyStreamTabOpen { get; set; }

        public Stream SelectedStreamTab
        {
            get { return _selectedStreamTab; }
            set
            {
                if (value == _selectedStreamTab) return;
                _selectedStreamTab = value;
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
