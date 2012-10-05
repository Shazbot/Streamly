using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using LeStreamsFace.Updater;
using MahApps.Metro.Controls;
using RestSharp;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Clipboard = System.Windows.Clipboard;
using Control = System.Windows.Controls.Control;
using DataGrid = System.Windows.Controls.DataGrid;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using TextBox = System.Windows.Controls.TextBox;
using ToolTip = System.Windows.Controls.ToolTip;

namespace LeStreamsFace
{
    /// <summary>
    /// Interaction logic for BlockedItemsWindow.xaml
    /// </summary>
    internal partial class StreamsListWindow : MetroWindow
    {
        private bool doFilterGames = true;
        private Func<bool> timeBlockCheck;

        public StreamsListWindow(Func<bool> timeBlockCheck)
        {
            InitializeComponent();
            this.timeBlockCheck = timeBlockCheck;

            NameScope.SetNameScope(windowCommands, NameScope.GetNameScope(this));

            TypeDescriptor.GetProperties(this.streamsDataGrid)["ItemsSource"].AddValueChanged(this.streamsDataGrid, new EventHandler(blockedItemsListBox_ItemsSourceChanged));
            this.AutoCheckFavoritesCheckBox.IsChecked = ConfigManager.Instance.AutoCheckFavorites;
            UpdateGameIconBackgrounds();
            this.streamsDataGrid.ItemsSource = StreamsManager.Streams;
            this.favoritesListBox.ItemsSource = ConfigManager.Instance.FavoriteStreams;

            var updaterViewModel = new UpdaterViewModel();
            Updater.DataContext = updaterViewModel;
            Updater.CheckForUpdateButton.Click += updaterViewModel.CheckForUpdate;
            updaterViewModel.CheckForUpdate();

            if (ConfigManager.Instance.SaveWindowPosition)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
            }
            else
            {
                Width = 1000;
                Height = 580;
            }
        }

        private void window_Closed(object sender, EventArgs e)
        {
            TypeDescriptor.GetProperties(this.streamsDataGrid)["ItemsSource"].RemoveValueChanged(this.streamsDataGrid, new EventHandler(blockedItemsListBox_ItemsSourceChanged));
            timeBlockCheck = null;
            Updater.CheckForUpdateButton.Click -= ((UpdaterViewModel)Updater.DataContext).CheckForUpdate;
            Updater.DataContext = null;

            gameStreamsPlot.Model = null;
            gameViewersPlot.Model = null;
        }

        private void blockedItemsListBox_ItemsSourceChanged(object sender, EventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(streamsDataGrid.ItemsSource);
            if (view != null)
            {
                using (view.DeferRefresh())
                {
                    view.Filter = Filter;

                    if (view.CanSort)
                    {
                        view.SortDescriptions.Clear();
                        view.SortDescriptions.Add(new SortDescription("IsFavorite", ListSortDirection.Descending));
                        view.SortDescriptions.Add(new SortDescription("Viewers", ListSortDirection.Descending));
                    }
                }
            }
        }

        private bool Filter(object o)
        {
            Stream stream = (Stream)o;

            if (stream.IsFavorite && MainWindow.WasTimeBlocking)
            {
                return false;
            }

            var searchText = searchTextBox.Text;
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

            if (doFilterGames)
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

        private void blockedItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //            ((DataGrid)sender).SelectedCells.Clear();
        }

        private void Path_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var starPath = ((Path)sender);

            Stream stream = (Stream)starPath.DataContext;
            stream.IsFavorite = !stream.IsFavorite;

            RefreshView();

            if (!stream.IsFavorite)
            {
                ConfigManager.Instance.FavoriteStreams.Remove(ConfigManager.Instance.FavoriteStreams.SingleOrDefault(stream1 => stream1.ChannelId == stream.ChannelId));
            }
            else
            {
                if (ConfigManager.Instance.FavoriteStreams.Where(stream1 => stream1.Site == stream.Site).All(stream1 => stream1.ChannelId != stream.ChannelId))
                {
                    ConfigManager.Instance.FavoriteStreams.Add(new FavoriteStream(stream.LoginNameTwtv, stream.ChannelId, stream.Site));
                }
            }
            ConfigManager.Instance.WriteConfigXml();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        private void ChangeTab_Click(object sender, RoutedEventArgs e)
        {
            var tabItem = (sender as FrameworkElement).Tag as TabItem;
            if (tabItem == streamsTabItem)
            {
                if (streamsTabItem.IsSelected)
                {
                    doFilterGames = !doFilterGames;

                    RefreshView();
                }
            }
            else if (tabItem == configTabItem)
            {
                DisabledTimeTextBox.Text = ConfigManager.Instance.FromSpan.ToString("hhmm") + '-' + ConfigManager.Instance.ToSpan.ToString("hhmm");
                BannedGamesTextBox.Text = ConfigManager.Instance.BannedGames.Aggregate((s, s1) => s + ", " + s1);
            }
            else if (tabItem == statsTabItem)
            {
                if (statsTabItem.IsSelected)
                {
                    // switch between stats for viewers and number of streams / game
                    foreach (var control in new Control[] { gameStreamsList, gameStreamsPlot, gameViewersList, gameViewersPlot })
                    {
                        control.Visibility = control.Visibility == Visibility.Visible
                                                     ? Visibility.Collapsed
                                                     : Visibility.Visible;
                    }
                }
            }
            else if (tabItem == aboutTabItem)
            {
            }

            tabItem.IsSelected = true;
        }

        private void TwitchFavorites_Click(object sender, RoutedEventArgs e)
        {
            string newBlockItem = this.twitchFavorites.Text;
            if (string.IsNullOrWhiteSpace(newBlockItem))
            {
                return;
            }

            XDocument xDoc;
            try
            {
                var channelFavsResponse = new RestClient("http://api.justin.tv/api/user/favorites/" + newBlockItem + ".xml").SinglePageResponse();
                xDoc = XDocument.Parse(channelFavsResponse.Content);
            }
            catch (WebException)
            {
                MessageBox.Show("Username doesn't exist (404)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var needToWriteConfig = false;
            foreach (XElement channel in xDoc.Element("channels").Descendants("channel"))
            {
                string channelId = null, twitchLogin = null;

                try
                {
                    channelId = channel.Element("id").Value;
                    twitchLogin = channel.Element("login").Value;
                }
                catch (NullReferenceException)
                {
                }

                if (ConfigManager.Instance.FavoriteStreams.Where(stream => stream.Site == StreamingSite.TwitchTv).All(stream => channelId != stream.ChannelId))
                {
                    ConfigManager.Instance.FavoriteStreams.Add(new FavoriteStream(twitchLogin, channelId, StreamingSite.TwitchTv));
                    StreamsManager.Streams.Where(stream => stream.Site == StreamingSite.TwitchTv && stream.ChannelId == channelId).ToList().ForEach(stream => stream.IsFavorite = true);
                    needToWriteConfig = true;
                }
            }

            if (needToWriteConfig)
            {
                MessageBox.Show("Successfully imported favorites.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ConfigManager.Instance.WriteConfigXml();
                RefreshView();
            }
            else
            {
                MessageBox.Show("No new streams to import.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void NameTitleTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var sendersStream = (Stream)((TextBlock)sender).DataContext;

            var ctrlDown = NativeMethods.IsButtonDown(Keys.ControlKey);
            if (ctrlDown)
            {
                Clipboard.SetText(sendersStream.GetUrl());
                return;
            }

            System.Diagnostics.Process.Start(sendersStream.GetUrl());
        }

        private void GameIconButton_Click(object sender, RoutedEventArgs e)
        {
            doFilterGames = true;

            FiltersEnum myFiltersEnum = (FiltersEnum)((Button)sender).Tag;

            if (StreamsManager.Filters[myFiltersEnum] == null)
            {
                StreamsManager.Filters[myFiltersEnum] = true;
            }
            else if (StreamsManager.Filters[myFiltersEnum] == true)
            {
                StreamsManager.Filters[myFiltersEnum] = false;
            }
            else if (StreamsManager.Filters[myFiltersEnum] == false)
            {
                StreamsManager.Filters[myFiltersEnum] = null;
            }

            UpdateGameIconBackgrounds();
            ConfigManager.Instance.WriteConfigXml();

            RefreshView();
        }

        private void UpdateGameIconBackgrounds()
        {
            foreach (Button button in windowCommands.FindChildren<Button>().Where(button => button.Tag is FiltersEnum))
            {
                var filterState = StreamsManager.Filters[(FiltersEnum)button.Tag];
                Brush background;
                if (filterState == null)
                {
                    background = null;
                }
                else if (filterState.Value)
                {
                    background = (Brush)new BrushConverter().ConvertFromString("#7F40B137");
                }
                else
                {
                    //                    background = (Brush)new BrushConverter().ConvertFromString("#7FB037B1");
                    background = (Brush)new BrushConverter().ConvertFromString("#7FC61D0C");
                }
                button.Background = background;
            }

            SetGameIconBackground();
        }

        private void SetGameIconBackground()
        {
            AdornerLayer adLayer = AdornerLayer.GetAdornerLayer(streamsDataGrid);
            if (adLayer == null) return;

            var currentIconFilters = StreamsManager.Filters.Where(pair => pair.Value ?? false);
            if (currentIconFilters.Count() == 1)
            {
                var brush = new ImageBrush();
                var iconConverter = new GameNameToIconUriConverter();

                var filteredGameName = ((DescriptionAttribute)typeof(FiltersEnum).GetMember(currentIconFilters.FirstOrDefault().Key.ToString())[0].GetCustomAttributes(
                                 typeof(DescriptionAttribute), false)[0]).Description;
                var iconUri = iconConverter.Convert(filteredGameName, null, null, null) as Uri;
                if (iconUri == null) return;

                brush.ImageSource = new BitmapImage(iconUri);
                brush.Stretch = Stretch.None;

                if (!adLayer.FindChildren<GameIconAdorner>().Any())
                {
                    adLayer.Add(new GameIconAdorner(streamsDataGrid, brush));
                }
            }
            else
            {
                adLayer.FindChildren<GameIconAdorner>().ToList().ForEach(adLayer.Remove);
            }
        }

        private void streamsDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            SetGameIconBackground();
        }

        private void DisabledTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var disabledTime = ((TextBox)sender).Text;
                TimeSpan fromSpan = new TimeSpan(0, 0, 0);
                TimeSpan toSpan = new TimeSpan(0, 0, 0);

                if (!string.IsNullOrWhiteSpace(disabledTime))
                {
                    string from = disabledTime.Split('-')[0];
                    string to = disabledTime.Split('-')[1];

                    fromSpan = new TimeSpan(int.Parse(from.Substring(0, 2)), int.Parse(from.Substring(2, 2)), 0);
                    toSpan = new TimeSpan(int.Parse(to.Substring(0, 2)), int.Parse(to.Substring(2, 2)), 0);

                    if (fromSpan.TotalSeconds != 0 && toSpan.TotalSeconds != 0)
                    {
                        if (fromSpan.CompareTo(toSpan) == 0)
                        {
                            return;
                        }
                        if (fromSpan.CompareTo(toSpan) != -1)
                        {
                            throw new ArgumentException();
                        }
                    }
                }
                ConfigManager.Instance.FromSpan = fromSpan;
                ConfigManager.Instance.ToSpan = toSpan;
                ConfigManager.Instance.WriteConfigXml();
                timeBlockCheck();
            }
            catch (Exception)
            {
            }
        }

        private void AutoCheckFavoritesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Instance.AutoCheckFavorites = ((CheckBox)sender).IsChecked.Value;
            ConfigManager.Instance.WriteConfigXml();
        }

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            changeTabButtons.Visibility = e.NewSize.Width <= 315 ? Visibility.Collapsed : Visibility.Visible;

            gameIconsPanel.Visibility = e.NewSize.Width <= 465 ? Visibility.Collapsed : Visibility.Visible;

            this.Title = e.NewSize.Width <= 700 ? string.Empty : "Do you even watch streams?";
        }

        private void UnfavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            var sendersStream = (FavoriteStream)((Button)sender).DataContext;

            ConfigManager.Instance.FavoriteStreams.Remove(sendersStream);

            StreamsManager.Streams.Where(stream => stream.ChannelId == sendersStream.ChannelId).ToList().ForEach(
                stream => stream.IsFavorite = false);

            RefreshView();
            ConfigManager.Instance.WriteConfigXml();
        }

        public void RefreshView(object sender = null, DataTransferEventArgs dataTransferEventArgs = null)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(streamsDataGrid.ItemsSource);
            if (view != null)
            {
                view.Refresh();
            }
        }

        private void BannedGamesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var bannedGamesText = ((TextBox)sender).Text;
                if (!string.IsNullOrWhiteSpace(bannedGamesText))
                {
                    var bannedGames = bannedGamesText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
                    ConfigManager.Instance.BannedGames = bannedGames.ToList();
                }
                else
                {
                    ConfigManager.Instance.BannedGames.Clear();
                }
                ConfigManager.Instance.WriteConfigXml();
                RefreshView();
            }
            catch (Exception)
            {
            }
        }

        private void window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!streamsTabItem.IsSelected)
            {
                return;
            }

            if (e.Key == Key.Escape)
            {
                streamsDataGrid.Focus();
                searchTextBox.Text = string.Empty;
                searchPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void window_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!streamsTabItem.IsSelected)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(e.Text))
            {
                var char1 = e.Text[0];
                if (char1 == 27)
                {
                    return;
                }
                searchPanel.Visibility = Visibility.Visible;
                searchTextBox.Focus();
            }
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ((TextBox)sender).Text;
            if (!string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                searchPanel.Visibility = Visibility.Visible;
            }
            else
            {
                streamsDataGrid.Focus();
                searchPanel.Visibility = Visibility.Collapsed;
            }

            RefreshView();
        }

        private void searchTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Text))
            {
                var char1 = e.Text[0];
                if (char1 == 27)
                {
                    streamsDataGrid.Focus();
                    searchPanel.Visibility = Visibility.Collapsed;
                    return;
                }
            }
        }

        private void searchTextBox_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Text))
            {
                searchPanel.Visibility = Visibility.Visible;
            }
            else
            {
                streamsDataGrid.Focus();
                searchPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void window_SourceInitialized(object sender, EventArgs e)
        {
            if (ConfigManager.Instance.PinToDesktop)
            {
                var handle = new WindowInteropHelper(this).Handle;
                IntPtr hwndParent = NativeMethods.FindWindow("ProgMan", null);
                NativeMethods.SetParent(handle, hwndParent);
            }
        }

        private void window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void RowMouseoverEvent(object sender, MouseEventArgs e)
        {
            if (refuseRowMouseoverSelect)
            {
                return;
            }

            var rowSender = (DataGridRow)sender;
            streamsDataGrid.SelectedItem = rowSender.DataContext;
            streamsDataGrid.ScrollIntoView(rowSender.DataContext);

            if (searchPanel.Visibility == Visibility.Collapsed)
            {
                rowSender.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private bool refuseRowMouseoverSelect;

        private void streamsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                refuseRowMouseoverSelect = true;
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (streamsDataGrid.SelectedItem != null)
                {
                    var selectedStream = (Stream)streamsDataGrid.SelectedItem;
                    System.Diagnostics.Process.Start(selectedStream.GetUrl());
                }
            }
        }

        private readonly DispatcherTimer mouseoverTimer = new DispatcherTimer(DispatcherPriority.Send);

        private void streamsDataGrid_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (!mouseoverTimer.IsEnabled)
                {
                    mouseoverTimer.Interval = TimeSpan.FromMilliseconds(100);
                    mouseoverTimer.Tick += (s, ev) =>
                                                {
                                                    mouseoverTimer.Stop();
                                                    refuseRowMouseoverSelect = false;
                                                };
                    mouseoverTimer.Start();
                }
            }
        }

        private void WrapPanel_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}