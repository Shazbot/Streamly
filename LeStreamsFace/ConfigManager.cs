using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Resources;
using System.Xml;
using System.Xml.Linq;
using OxyPlot;
using MessageBox = System.Windows.MessageBox;

namespace LeStreamsFace
{
    internal class ConfigManager
    {
        public static PlotModel StreamsPerGamePlotModel
        {
            get { return _streamsPerGamePlotModel; }
            set { _streamsPerGamePlotModel = value; OnStaticPropertyChanged(GetVariableName(() => StreamsPerGamePlotModel)); }
        }

        private static PlotModel _streamsPerGamePlotModel;

        public static PlotModel ViewersPerGamePlotModel
        {
            get { return _viewersPerGamePlotModel; }
            set { _viewersPerGamePlotModel = value; OnStaticPropertyChanged(GetVariableName(() => ViewersPerGamePlotModel)); }
        }

        private static PlotModel _viewersPerGamePlotModel;

        public static void UpdatePlotModel(IEnumerable<Stream> gameStreams)
        {
            try
            {
                if (!gameStreams.Any()) return;

                const int showFirst = 15;
                // create a copy
                var streams = gameStreams.ToList();

                var groupedByViewers =
                    streams.ToList().GroupBy(stream => stream.GameName).OrderByDescending(
                        grouping => grouping.Select(stream => stream.Viewers).Sum());
                var groupedByGame = streams.ToList().GroupBy(stream => stream.GameName).OrderByDescending(grouping => grouping.Count());

                var model = new PlotModel("Number of streams per game");
                var ps = new PieSeries();
                foreach (IGrouping<string, Stream> grouping in groupedByGame.Take(showFirst))
                {
                    var pieSliceLabel = grouping.Key;

                    //                if (string.IsNullOrWhiteSpace(grouping.Key)) pieSliceLabel = "Unknown";
                    if (string.IsNullOrWhiteSpace(grouping.Key)) continue;
                    ps.Slices.Add(new PieSlice(pieSliceLabel, grouping.Count()));
                }
                var theRestOfGames = groupedByGame.Skip(showFirst);
                ps.Slices.Add(new PieSlice("Other", theRestOfGames.Select(grouping => grouping.Count()).Aggregate((i, i1) => i + i1)));

                //            ps.Slices.ForEach(slice => slice.Label = slice.Value.ToString() + " " + slice.Label);
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

                model = new PlotModel("Total viewers per game");
                ps = new PieSeries();
                foreach (IGrouping<string, Stream> grouping in groupedByViewers.Take(showFirst))
                {
                    var pieSliceLabel = grouping.Key;

                    //                if (string.IsNullOrWhiteSpace(grouping.Key)) pieSliceLabel = "Unknown";
                    if (string.IsNullOrWhiteSpace(grouping.Key)) continue;
                    ps.Slices.Add(new PieSlice(pieSliceLabel, grouping.Sum(stream => stream.Viewers)));
                }
                theRestOfGames = groupedByViewers.Skip(showFirst);
                ps.Slices.Add(new PieSlice("Other", theRestOfGames.Select(grouping => grouping.Sum(stream => stream.Viewers)).Sum()));

                //            ps.Slices.ForEach(slice => slice.Label = slice.Value.ToString() + " " + slice.Label);
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

        static ConfigManager()
        {
            //            UpdatePlotModel();

            //            var temp = new PlotModel("Square wave");
            //            var ls = new LineSeries("sin(x)+sin(3x)/3+sin(5x)/5+...");
            //            int n = 10;
            //            for (double x = -10; x < 10; x += 0.0001)
            //            {
            //                double y = 0;
            //                for (int i = 0; i < n; i++)
            //                {
            //                    int j = i * 2 + 1;
            //                    y += Math.Sin(j * x) / j;
            //                }
            //                ls.Points.Add(new DataPoint(x, y));
            //            }
            //            temp.Series.Add(ls);
            //            temp.Axes.Add(new LinearAxis(AxisPosition.Left, -4, 4));
            //            temp.Axes.Add(new LinearAxis(AxisPosition.Bottom));
            //            StreamsPerGamePlotModel = temp;
        }

        public static int SamplingInterval = 60;

        public static TimeSpan FromSpan = new TimeSpan(0, 0, 0);
        public static TimeSpan ToSpan = new TimeSpan(0, 0, 0);

        public static readonly OptimizedObservableCollection<FavoriteStream> FavoriteStreams = new OptimizedObservableCollection<FavoriteStream>();
        public static List<string> BannedGames = new List<string>();

        private const string ConfigFileName = "config.xml";
        private const string XmlVersion = "1.0";

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static void OnStaticPropertyChanged(string propertyName)
        {
            EventHandler<PropertyChangedEventArgs> handler = StaticPropertyChanged;
            if (handler != null)
                handler(null, new PropertyChangedEventArgs(propertyName));
        }

        public static int TriageStreams
        {
            get { return _triageStreams; }
            set
            {
                if (_triageStreams == value) return;
                _triageStreams = value;
                OnStaticPropertyChanged(GetVariableName(() => TriageStreams));
                WriteConfigXml();
            }
        }

        public static int NotificationTimeout
        {
            get { return _notificationTimeout; }
            set
            {
                if (_notificationTimeout == value) return;
                _notificationTimeout = value;
                OnStaticPropertyChanged(GetVariableName(() => NotificationTimeout));
                WriteConfigXml();
            }
        }

        public static bool HideTitleBar
        {
            get { return _hideTitleBar; }
            set
            {
                if (_hideTitleBar == value) return;
                _hideTitleBar = value;
                OnStaticPropertyChanged(GetVariableName(() => HideTitleBar));
                WriteConfigXml();
            }
        }

        public static bool PinToDesktop
        {
            get { return _pinToDesktop; }
            set
            {
                if (_pinToDesktop == value) return;
                _pinToDesktop = value;
                OnStaticPropertyChanged(GetVariableName(() => PinToDesktop));
                WriteConfigXml();
            }
        }

        public static bool SaveWindowPosition
        {
            get { return _saveWindowPosition; }
            set
            {
                if (_saveWindowPosition == value) return;
                _saveWindowPosition = value;
                OnStaticPropertyChanged(GetVariableName(() => SaveWindowPosition));
                WriteConfigXml();
            }
        }

        public static double WinLeft
        {
            get { return _winLeft; }
            set
            {
                if (_winLeft == value) return;
                _winLeft = value;
                OnStaticPropertyChanged(GetVariableName(() => WinLeft));
                WriteConfigXml();
            }
        }

        public static double WinTop
        {
            get { return _winTop; }
            set
            {
                if (_winTop == value) return;
                _winTop = value;
                OnStaticPropertyChanged(GetVariableName(() => WinTop));
                WriteConfigXml();
            }
        }

        public static double WinOpacity
        {
            get { return _winOpacity; }
            set
            {
                if (_winOpacity == value) return;
                _winOpacity = value;
                OnStaticPropertyChanged(GetVariableName(() => WinOpacity));
                WriteConfigXml();
            }
        }

        public static double WinWidth
        {
            get { return _winWidth; }
            set
            {
                if (_winWidth == value) return;
                _winWidth = value;
                OnStaticPropertyChanged(GetVariableName(() => WinWidth));
                WriteConfigXml();
            }
        }

        public static double WinHeight
        {
            get { return _winHeight; }
            set
            {
                if (_winHeight == value) return;
                _winHeight = value;
                OnStaticPropertyChanged(GetVariableName(() => WinHeight));
                WriteConfigXml();
            }
        }

        private static double _winWidth = 1000;
        private static double _winHeight = 580;
        private static double _winOpacity = 1.0;
        private static double _winLeft = 0;
        private static double _winTop = 0;
        private static bool _saveWindowPosition;
        private static bool _hideTitleBar;
        private static bool _pinToDesktop;
        private static int _notificationTimeout = 20;
        private static int _triageStreams = 20;

        private static bool initialConfigReadCompleted = false;
        public static bool AutoCheckFavorites = true;

        public static void ReadConfigXml()
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
                HideTitleBar = bool.Parse(XAppSettings.Element(GetVariableName(() => HideTitleBar)).Value);
                PinToDesktop = bool.Parse(XAppSettings.Element(GetVariableName(() => PinToDesktop)).Value);
                SaveWindowPosition = bool.Parse(XAppSettings.Element(GetVariableName(() => SaveWindowPosition)).Value);
                WinTop = double.Parse(XAppSettings.Element(GetVariableName(() => WinTop)).Value);
                WinLeft = double.Parse(XAppSettings.Element(GetVariableName(() => WinLeft)).Value);
                WinOpacity = double.Parse(XAppSettings.Element(GetVariableName(() => WinOpacity)).Value);
                WinWidth = double.Parse(XAppSettings.Element(GetVariableName(() => WinWidth)).Value);
                WinHeight = double.Parse(XAppSettings.Element(GetVariableName(() => WinHeight)).Value);

                BannedGames = xDoc.Element("Config").Element(GetVariableName(() => BannedGames)).Value.Split(',').Select(s => s.Trim()).ToList();

                foreach (XElement xFilter in xDoc.Element("Config").Element("Filters").Elements())
                {
                    StreamsManager.Filters[(FiltersEnum)Enum.Parse(typeof(FiltersEnum), xFilter.Name.ToString())] = bool.Parse(xFilter.Value);
                }

                var favorites = xDoc.Element("Config").Element("Favorites");

                favorites.Element("TwitchTv").Descendants("Stream").Select(
                    element => new FavoriteStream(element.Attribute("Name").Value, element.Value, StreamingSite.TwitchTv)).
                        ToList().ForEach(stream => FavoriteStreams.Add(stream));
                favorites.Element("OwnedTv").Descendants("Stream").Select(
                    element => new FavoriteStream(element.Attribute("Name").Value, element.Value, StreamingSite.OwnedTv))
                        .ToList().ForEach(stream => FavoriteStreams.Add(stream));

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

        public static void WriteConfigXml()
        {
            if (!initialConfigReadCompleted)
            {
                return;
            }

            XDocument xDoc = new XDocument(
                new XDeclaration(XmlVersion, "utf-8", "yes"),
                new XElement("Config",
                    new XElement("AppSettings"), new XElement("Filters"), new XElement(GetVariableName(() => BannedGames)), new XElement("TimeBlock"), new XElement("Favorites", new XElement("TwitchTv"), new XElement("OwnedTv"))
                )
            );

            var appSettings = xDoc.Element("Config").Element("AppSettings");
            appSettings.Add(new XElement(GetVariableName(() => SamplingInterval), SamplingInterval));
            appSettings.Add(new XElement(GetVariableName(() => NotificationTimeout), NotificationTimeout));
            appSettings.Add(new XElement(GetVariableName(() => TriageStreams), TriageStreams));
            appSettings.Add(new XElement(GetVariableName(() => AutoCheckFavorites), AutoCheckFavorites));
            appSettings.Add(new XElement(GetVariableName(() => HideTitleBar), HideTitleBar));
            appSettings.Add(new XElement(GetVariableName(() => PinToDesktop), PinToDesktop));
            appSettings.Add(new XElement(GetVariableName(() => SaveWindowPosition), SaveWindowPosition));
            appSettings.Add(new XElement(GetVariableName(() => WinTop), WinTop));
            appSettings.Add(new XElement(GetVariableName(() => WinLeft), WinLeft));
            appSettings.Add(new XElement(GetVariableName(() => WinOpacity), WinOpacity));
            appSettings.Add(new XElement(GetVariableName(() => WinWidth), WinWidth));
            appSettings.Add(new XElement(GetVariableName(() => WinHeight), WinHeight));

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

            var ownedFavs = xDoc.Element("Config").Element("Favorites").Element("OwnedTv");
            foreach (Stream stream in FavoriteStreams.Where(stream => stream.Site == StreamingSite.OwnedTv))
            {
                XElement streamItem = new XElement("Stream", stream.ChannelId);
                streamItem.SetAttributeValue("Name", stream.LoginNameTwtv);
                ownedFavs.Add(streamItem);
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
    }
}