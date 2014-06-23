using OxyPlot;
using OxyPlot.Series;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Resources;
using System.Xml;
using System.Xml.Linq;
using MessageBox = System.Windows.MessageBox;

namespace LeStreamsFace
{
    [ImplementPropertyChanged]
    internal class ConfigManager : INotifyPropertyChanged
    {
        public PlotModel StreamsPerGamePlotModel { get; set; }

        public PlotModel ViewersPerGamePlotModel { get; set; }

        public OptimizedObservableCollection<Tuple<string, int>> StreamsPerGame { get; set; }

        public OptimizedObservableCollection<Tuple<string, int>> ViewersPerGame { get; set; }

        public void UpdatePlotModel(IEnumerable<Stream> gameStreams)
        {
            try
            {
                if (!gameStreams.Any()) return;

                const int showFirst = 15;
                // create a copy
                var streams = gameStreams.ToList();

                var groupedByViewers = streams.GroupBy(stream => stream.GameName).OrderByDescending(grouping => grouping.Select(stream => stream.Viewers).Sum());
                var groupedByGame = streams.GroupBy(stream => stream.GameName).OrderByDescending(grouping => grouping.Count());

                var model = new PlotModel();
                model.Title = "Number of streams per game";
                var ps = new PieSeries();

                foreach (IGrouping<string, Stream> grouping in groupedByGame.Take(showFirst))
                {
                    var pieSliceLabel = grouping.Key;

                    if (string.IsNullOrWhiteSpace(grouping.Key)) continue;
                    ps.Slices.Add(new PieSlice(pieSliceLabel, grouping.Count()));
                }
                var theRestOfGamesViewers = groupedByGame.Skip(showFirst).Select(grouping => grouping.Count()).Aggregate((i, i1) => i + i1);
                ps.Slices.Add(new PieSlice("Other", theRestOfGamesViewers));

                var gameNameCountTuples = groupedByGame.Select(grouping => new Tuple<string, int>(grouping.Key, grouping.Count())).ToList();
                gameNameCountTuples.RemoveAll(tuple => string.IsNullOrWhiteSpace(tuple.Item1));
                StreamsPerGame = new OptimizedObservableCollection<Tuple<string, int>>();
                StreamsPerGame.AddRange(gameNameCountTuples.OrderByDescending(tuple => tuple.Item2));

                ps.AreInsideLabelsAngled = true;
                ps.InnerDiameter = 0;
                ps.ExplodedDistance = 0.0;
                ps.Stroke = OxyColors.White;
                ps.StrokeThickness = 2.0;
                ps.InsideLabelPosition = 0.8;
                ps.AngleSpan = 360;
                ps.StartAngle = 0;
                model.Series.Add(ps);

                StreamsPerGamePlotModel = model;

                model = new PlotModel();
                model.Title = "Total viewers per game";
                ps = new PieSeries();
                foreach (IGrouping<string, Stream> grouping in groupedByViewers.Take(showFirst))
                {
                    var pieSliceLabel = grouping.Key;

                    if (string.IsNullOrWhiteSpace(grouping.Key)) continue;
                    ps.Slices.Add(new PieSlice(pieSliceLabel, grouping.Sum(stream => stream.Viewers)));
                }
                theRestOfGamesViewers = groupedByViewers.Skip(showFirst).Select(grouping => grouping.Count()).Aggregate((i, i1) => i + i1);
                ps.Slices.Add(new PieSlice("Other", theRestOfGamesViewers));

                gameNameCountTuples = groupedByViewers.Select(grouping => new Tuple<string, int>(grouping.Key, grouping.Sum(stream => stream.Viewers))).ToList();
                gameNameCountTuples.RemoveAll(tuple => string.IsNullOrWhiteSpace(tuple.Item1));
                ViewersPerGame = new OptimizedObservableCollection<Tuple<string, int>>();
                ViewersPerGame.AddRange(gameNameCountTuples.OrderByDescending(tuple => tuple.Item2));

                ps.AreInsideLabelsAngled = true;
                ps.InnerDiameter = 0;
                ps.ExplodedDistance = 0.0;
                ps.Stroke = OxyColors.White;
                ps.StrokeThickness = 2.0;
                ps.InsideLabelPosition = 0.8;
                ps.AngleSpan = 360;
                ps.StartAngle = 0;
                model.Series.Add(ps);

                ViewersPerGamePlotModel = model;
            }
            catch (Exception)
            {
            }
        }

        private static volatile ConfigManager _instance;
        private static object syncRoot = new Object();

        private ConfigManager()
        {
            FavoriteStreams = new OptimizedObservableCollection<FavoriteStream>();
            AutoCheckFavorites = true;
            Offline = false;
            StreamOpeningProcedure = StreamOpeningProcedure.Tab;
            LivestreamerArguments = "best";

            Uri uri = new Uri("pack://application:,,,/Resources/streamHtml.txt");
            StreamResourceInfo streamResourceInfo = Application.GetResourceStream(uri);
            StreamHtml = new StreamReader(streamResourceInfo.Stream).ReadToEnd();
        }

        public string StreamHtml { get; private set; }

        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (syncRoot)
                    {
                        if (_instance == null)
                            _instance = new ConfigManager();
                    }
                }

                return _instance;
            }
        }

        public int SamplingInterval = 60;

        public TimeSpan FromSpan = new TimeSpan(0, 0, 0);
        public TimeSpan ToSpan = new TimeSpan(0, 0, 0);

        public OptimizedObservableCollection<FavoriteStream> FavoriteStreams { get; private set; }

        public List<string> BannedGames = new List<string>();

        private const string ConfigFileName = "config.xml";
        private const string XmlVersion = "1.0";

        public int TriageStreams
        {
            get { return _triageStreams; }
            set
            {
                if (_triageStreams == value) return;
                _triageStreams = value;
                WriteConfigXml();
            }
        }

        public int NotificationTimeout
        {
            get { return _notificationTimeout; }
            set
            {
                if (_notificationTimeout == value) return;
                _notificationTimeout = value;
                WriteConfigXml();
            }
        }

        public bool SaveWindowPosition
        {
            get { return _saveWindowPosition; }
            set
            {
                if (_saveWindowPosition == value) return;
                _saveWindowPosition = value;
                WriteConfigXml();
            }
        }

        public double WinLeft
        {
            get { return _winLeft; }
            set
            {
                if (_winLeft == value) return;
                _winLeft = value;
                WriteConfigXml();
            }
        }

        public double WinTop
        {
            get { return _winTop; }
            set
            {
                if (_winTop == value) return;
                _winTop = value;
                WriteConfigXml();
            }
        }

        public double WinWidth
        {
            get { return _winWidth; }
            set
            {
                if (_winWidth == value) return;
                _winWidth = value;
                WriteConfigXml();
            }
        }

        public double WinHeight
        {
            get { return _winHeight; }
            set
            {
                if (_winHeight == value) return;
                _winHeight = value;
                WriteConfigXml();
            }
        }

        public bool Offline { get; set; }

        private double _winWidth = 1000;
        private double _winHeight = 580;
        private double _winLeft = 0;
        private double _winTop = 0;
        private bool _saveWindowPosition;
        private int _notificationTimeout = 20;
        private int _triageStreams = 20;

        private bool initialConfigReadCompleted = false;
        private bool _autoCheckFavorites;
        private StreamOpeningProcedure _streamOpeningProcedure;

        public bool AutoCheckFavorites
        {
            get { return _autoCheckFavorites; }
            set
            {
                if (value == _autoCheckFavorites) return;
                _autoCheckFavorites = value;
                WriteConfigXml();
            }
        }

        public StreamOpeningProcedure StreamOpeningProcedure
        {
            get { return _streamOpeningProcedure; }
            set
            {
                if (value == _streamOpeningProcedure) return;
                _streamOpeningProcedure = value;
                WriteConfigXml();
            }
        }

        private string _livestreamerArguments;

        public string LivestreamerArguments
        {
            get { return _livestreamerArguments; }
            set
            {
                if (value == _livestreamerArguments) return;
                _livestreamerArguments = value;
                WriteConfigXml();
            }
        }

        public void ReadConfigXml()
        {
            bool configMissing = !File.Exists(ConfigFileName);

            try
            {
                XDocument xDoc;
                if (!configMissing)
                {
                    xDoc = XDocument.Load(ConfigFileName, LoadOptions.PreserveWhitespace);
                }
                else
                {
                    Uri uri = new Uri("pack://application:,,,/Resources/" + ConfigFileName);
                    StreamResourceInfo streamResourceInfo = Application.GetResourceStream(uri);
                    xDoc = XDocument.Load(streamResourceInfo.Stream);
                }

                var XAppSettings = xDoc.Element("Config").Element("AppSettings");
                SamplingInterval = int.Parse(XAppSettings.Element(GetVariableName(() => SamplingInterval)).Value);
                NotificationTimeout = int.Parse(XAppSettings.Element(GetVariableName(() => NotificationTimeout)).Value);
                TriageStreams = int.Parse(XAppSettings.Element(GetVariableName(() => TriageStreams)).Value);
                AutoCheckFavorites = bool.Parse(XAppSettings.Element(GetVariableName(() => AutoCheckFavorites)).Value);
                SaveWindowPosition = bool.Parse(XAppSettings.Element(GetVariableName(() => SaveWindowPosition)).Value);
                WinTop = double.Parse(XAppSettings.Element(GetVariableName(() => WinTop)).Value);
                WinLeft = double.Parse(XAppSettings.Element(GetVariableName(() => WinLeft)).Value);
                WinWidth = double.Parse(XAppSettings.Element(GetVariableName(() => WinWidth)).Value);
                WinHeight = double.Parse(XAppSettings.Element(GetVariableName(() => WinHeight)).Value);
                LivestreamerArguments = (string)XAppSettings.Element(GetVariableName(() => LivestreamerArguments)) ?? "best";
                StreamOpeningProcedure = (StreamOpeningProcedure)Enum.Parse(typeof(StreamOpeningProcedure), (string)XAppSettings.Element(GetVariableName(() => StreamOpeningProcedure)) ?? "Browser");

                BannedGames = xDoc.Element("Config").Element(GetVariableName(() => BannedGames)).Value.Split(',').Select(s => s.Trim()).ToList();

                foreach (XElement xFilter in xDoc.Element("Config").Element("Filters").Elements())
                {
                    StreamsManager.Filters[(FiltersEnum)Enum.Parse(typeof(FiltersEnum), xFilter.Name.ToString())] = bool.Parse(xFilter.Value);
                }

                var favorites = xDoc.Element("Config").Element("Favorites");

                favorites.Element("TwitchTv").Descendants("Stream").Select(
                    element => new FavoriteStream(element.Attribute("Name").Value, element.Value, StreamingSite.TwitchTv)).
                        ToList().ForEach(stream => FavoriteStreams.Add(stream));

                var timeBlock = xDoc.Element("Config").Element("TimeBlock");
                FromSpan = TimeSpan.Parse(timeBlock.Element(GetVariableName(() => FromSpan)).Value);
                ToSpan = TimeSpan.Parse(timeBlock.Element(GetVariableName(() => ToSpan)).Value);
            }
            catch (Exception)
            {
                MessageBox.Show("Invalid config.xml", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                App.ExitApp();
            }

            initialConfigReadCompleted = true;

            if (configMissing)
            {
                WriteConfigXml();
            }
        }

        public void WriteConfigXml()
        {
            if (!initialConfigReadCompleted)
            {
                return;
            }

            XDocument xDoc = new XDocument(
                new XDeclaration(XmlVersion, "utf-8", "yes"),
                new XElement("Config",
                    new XElement("AppSettings"),
                    new XElement("Filters"),
                    new XElement(GetVariableName(() => BannedGames)),
                    new XElement("TimeBlock"),
                    new XElement("Favorites", new XElement("TwitchTv")))
            );

            var appSettings = xDoc.Element("Config").Element("AppSettings");
            appSettings.Add(new XElement(GetVariableName(() => SamplingInterval), SamplingInterval));
            appSettings.Add(new XElement(GetVariableName(() => NotificationTimeout), NotificationTimeout));
            appSettings.Add(new XElement(GetVariableName(() => TriageStreams), TriageStreams));
            appSettings.Add(new XElement(GetVariableName(() => AutoCheckFavorites), AutoCheckFavorites));
            appSettings.Add(new XElement(GetVariableName(() => SaveWindowPosition), SaveWindowPosition));
            appSettings.Add(new XElement(GetVariableName(() => WinTop), WinTop));
            appSettings.Add(new XElement(GetVariableName(() => WinLeft), WinLeft));
            appSettings.Add(new XElement(GetVariableName(() => WinWidth), WinWidth));
            appSettings.Add(new XElement(GetVariableName(() => WinHeight), WinHeight));
            appSettings.Add(new XElement(GetVariableName(() => LivestreamerArguments), LivestreamerArguments));
            appSettings.Add(new XElement(GetVariableName(() => StreamOpeningProcedure), StreamOpeningProcedure));

            xDoc.Element("Config").Element(GetVariableName(() => BannedGames)).Value = BannedGames.Any() ? BannedGames.Aggregate((s, s1) => s + ',' + s1) : string.Empty;

            var filters = xDoc.Element("Config").Element("Filters");
            foreach (KeyValuePair<FiltersEnum, bool?> keyValuePair in StreamsManager.Filters.Where(pair => pair.Value != null))
            {
                filters.Add(new XElement(keyValuePair.Key.ToString(), keyValuePair.Value));
            }

            var twitchFavs = xDoc.Element("Config").Element("Favorites").Element("TwitchTv");
            foreach (Stream stream in FavoriteStreams.Where(stream => stream.Site == StreamingSite.TwitchTv))
            {
                XElement streamItem = new XElement("Stream", stream.ChannelId);
                streamItem.SetAttributeValue("Name", stream.LoginNameTwtv);
                twitchFavs.Add(streamItem);
            }

            var blockSettings = xDoc.Element("Config").Element("TimeBlock");
            blockSettings.Add(new XElement(GetVariableName(() => FromSpan), FromSpan.ToString()));
            blockSettings.Add(new XElement(GetVariableName(() => ToSpan), ToSpan.ToString()));

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.IndentChars = "\t";
            settings.Indent = true;

            using (XmlWriter xw = XmlWriter.Create(ConfigFileName, settings))
            {
                xDoc.Save(xw);
            }
        }

        private static string GetVariableName<T>(Expression<Func<T>> expression)
        {
            var body = ((MemberExpression)expression.Body);

            return body.Member.Name;
        }

#pragma warning disable 67

        public event PropertyChangedEventHandler PropertyChanged;

#pragma warning restore 67
    }
}