using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using Control = System.Windows.Controls.Control;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace LeStreamsFace
{
    /// <summary>
    /// Interaction logic for BlockedItemsWindow.xaml
    /// </summary>
    internal partial class StreamsListWindow : MetroWindow
    {
        private bool doFilterGames = true;
        private Func<bool> timeBlockCheck;
        private StreamsListViewModel vm;

        public StreamsListWindow(Func<bool> timeBlockCheck)
        {
            InitializeComponent();
            this.timeBlockCheck = timeBlockCheck;

            NameScope.SetNameScope(windowCommands, NameScope.GetNameScope(this));

            TypeDescriptor.GetProperties(this.streamsDataGrid)["ItemsSource"].AddValueChanged(this.streamsDataGrid, new EventHandler(blockedItemsListBox_ItemsSourceChanged));
            //          MOVED TO BINDING  this.AutoCheckFavoritesCheckBox.IsChecked = ConfigManager.Instance.AutoCheckFavorites;
            UpdateGameIconBackgrounds();
            this.streamsDataGrid.ItemsSource = StreamsManager.Streams;
            //this.favoritesListBox.ItemsSource = ConfigManager.Instance.FavoriteStreams;

            if (ConfigManager.Instance.SaveWindowPosition)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
            }
            else
            {
                Width = 1000;
                Height = 580;
            }

            DataContext = vm = new StreamsListViewModel(this);

            var url = @"<object type=""application/x-shockwave-flash"" height=""100%"" width=""100%"" style=""overflow:hidden; width:100%; height:100%; margin:0; padding:0; border:0;"" id=""live_embed_player_flash"" data=""http://www.twitch.tv/widgets/live_embed_player.swf?channel=" + "wingsofdeath" + @""" bgcolor=""#000000""><param name=""allowFullScreen"" value=""false"" /><param name=""allowScriptAccess"" value=""always"" /><param name=""allowNetworking"" value=""all"" /><param name=""movie"" value=""http://www.twitch.tv/widgets/live_embed_player.swf"" /><param name=""flashvars"" value=""hostname=www.twitch.tv&channel=" + "wingsofdeath" + @"&auto_play=true&start_volume=25"" /></object>";
            //            url = @"<div style=""overflow:hidden;"">" + url + @"</div>";
            //            gamesFlyout.IsOpen = false;
            //            cefFlyout.IsOpen = true;
            //	    cefWebView.browser.LoadHtml(url, "www.google.com");

            configTabItem.IsSelected = true;
            streamsTabItem.IsSelected = true;
        }

        private void window_Closed(object sender, EventArgs e)
        {
            TypeDescriptor.GetProperties(this.streamsDataGrid)["ItemsSource"].RemoveValueChanged(this.streamsDataGrid, new EventHandler(blockedItemsListBox_ItemsSourceChanged));
            timeBlockCheck = null;

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

        private void ChangeTab_Click(object sender, RoutedEventArgs e)
        {
            return;
            // TODO MOVE THIS TO VM
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
                //                                DisabledTimeTextBox.Text = ConfigManager.Instance.FromSpan.ToString("hhmm") + '-' + ConfigManager.Instance.ToSpan.ToString("hhmm");
                //                                BannedGamesTextBox.Text = ConfigManager.Instance.BannedGames.Aggregate((s, s1) => s + ", " + s1);
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
        }

        private void NameTitleTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var sendersStream = (Stream)((TextBlock)sender).DataContext;

            var ctrlDown = NativeMethods.IsButtonDown(Keys.ControlKey);
            if (ctrlDown)
            {
                try
                {
                    Clipboard.SetText(sendersStream.GetUrl());
                }
                catch (Exception)
                {
                }
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

        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            changeTabButtons.Visibility = e.NewSize.Width <= 315 ? Visibility.Collapsed : Visibility.Visible;

            gameIconsPanel.Visibility = e.NewSize.Width <= 465 ? Visibility.Collapsed : Visibility.Visible;

            this.Title = e.NewSize.Width <= 700 ? string.Empty : "More streams than you can shake a stick at";
        }

        public void RefreshView(object sender = null, DataTransferEventArgs dataTransferEventArgs = null)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(streamsDataGrid.ItemsSource);
            if (view != null)
            {
                view.Refresh();
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
            if (!streamsTabItem.IsSelected || Flyouts.GetChildObjects().Cast<Flyout>().Any(flyout => flyout.IsOpen))
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

        //        public new void DragMove()
        //        {
        //            var hs = (HwndSource)PresentationSource.FromVisual(this);
        //            if (WindowState == WindowState.Normal)
        //            {
        //                NativeMethods.SendMessage(hs.Handle, ((uint) NativeMethods.WindowMessages.WM_SYSCOMMAND), (IntPtr)0xf012, IntPtr.Zero);
        //                NativeMethods.SendMessage(hs.Handle, ((uint) NativeMethods.WindowMessages.WM_LBUTTONUP), IntPtr.Zero, IntPtr.Zero);
        //            }
        //        }

        private void RowMouseoverEvent(object sender, MouseEventArgs e)
        {
            if (refuseRowMouseoverSelect)
            {
                return;
            }

            try
            {
                var rowSender = (DataGridRow)sender;

                // give keyboard focus to the mouseovered row if it's not only partially visible, otherwise give it to the row before it
                if (IsWholeChildVisible(streamsDataGrid, rowSender))
                {
                    rowSender.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
                else
                {
                    var indexPreviousRow = streamsDataGrid.Items.IndexOf(rowSender.DataContext) - 1;
                    if (indexPreviousRow > -1)
                    {
                        var previousRow = streamsDataGrid.ItemContainerGenerator.ContainerFromIndex(indexPreviousRow) as DataGridRow;
                        if (previousRow != null) previousRow.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                }

                streamsDataGrid.SelectedItem = rowSender.DataContext;
            }
            catch (InvalidOperationException) // collection was changed during enumeration exception
            {
            }
        }

        private static bool IsWholeChildVisible(FrameworkElement container, FrameworkElement element)
        {
            if (!element.IsVisible) return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            var rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) && rect.Contains(bounds.BottomRight);
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

        public void ConfigTabMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //            e.Handled = true;
            //            try
            //            {
            //                DragMove();
            //            }
            //            catch (InvalidOperationException)
            //            {
            //            }
        }

        private async void GamesPanel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //            selectedGameFlyout.Header = name;
            //            selectedGamesPanel.ItemsSource = streams;
        }

        private async void SelectedGamesPanel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var stream = ((ListBox)sender).SelectedItem as Stream;
            if (stream == null)
            {
                return;
            }

            var newStreamingTab = new TabItem();
            newStreamingTab.Header = stream.LoginNameTwtv;
            newStreamingTab.Visibility = Visibility.Collapsed;
            newStreamingTab.Content = stream.LoginNameTwtv;
            tabControl.Items.Add(newStreamingTab);
            newStreamingTab.IsSelected = true;

            //
            //            cefWebView.WebBrowser.Address = url;

            //            var wings = @"<object type=""application/x-shockwave-flash"" height=""" + "100%" + @""" width=""" + "100%" + @""" id=""live_embed_player_flash"" data=""http://www.twitch.tv/widgets/live_embed_player.swf?channel=wingsofdeath"" bgcolor=""#000000""><param name=""allowFullScreen"" value=""true"" /><param name=""allowScriptAccess"" value=""always"" /><param name=""allowNetworking"" value=""all"" /><param name=""movie"" value=""http://www.twitch.tv/widgets/live_embed_player.swf"" /><param name=""flashvars"" value=""hostname=www.twitch.tv&channel=wingsofdeath&auto_play=true&start_volume=25"" /></object><a href=""http://www.twitch.tv/wingsofdeath"" style=""padding:2px 0px 4px; display:block; width:345px; font-weight:normal; font-size:10px;text-decoration:underline; text-align:center;"">Watch live video from Wingsofdeath on www.twitch.tv</a>";
            //            cefWebView.WebBrowser.LoadHtml(wings, "arst");// = wings;

            var url = @"<object type=""application/x-shockwave-flash"" height=""100%"" width=""100%"" style=""overflow:hidden; width:100%; height:100%; margin:0; padding:0; border:0;"" id=""live_embed_player_flash"" data=""http://www.twitch.tv/widgets/live_embed_player.swf?channel=" + stream.LoginNameTwtv + @""" bgcolor=""#000000""><param name=""allowFullScreen"" value=""false"" /><param name=""allowScriptAccess"" value=""always"" /><param name=""allowNetworking"" value=""all"" /><param name=""movie"" value=""http://www.twitch.tv/widgets/live_embed_player.swf"" /><param name=""flashvars"" value=""hostname=www.twitch.tv&channel=" + stream.LoginNameTwtv + @"&auto_play=true&start_volume=25"" /></object>";
            url = @"<div style=""overflow:hidden;"">" + url + @"</div>";
            gamesFlyout.IsOpen = false;
            selectedGameFlyout.IsOpen = false;

            //            cefWebView.webView.LoadHtml(url, stream.LoginNameTwtv);
            //            cefFlyout.IsOpen = true;

            //            cefWebView.webView.LoadHtml(wings, "about:blank");// = wings;
            // = wings;
            //            cefWebView.webView.WebBrowser.LoadHtml(url, "about:blank");// = wings;
            //            cefWebView.webView.WebBrowser.Address = "www.google.com";

            //            var dialog = (BaseMetroDialog)this.Resources["LoadingDialog"];
            //            if (cefWebView.webView == null || cefWebView.WebBrowser == null)
            //            {
            //                await this.ShowMetroDialogAsync(dialog);
            //                while (cefWebView.webView == null || cefWebView.WebBrowser == null)
            //                {
            //                    await TaskEx.Delay(200);
            //                }
            //                await this.HideMetroDialogAsync(dialog);
            //            }
        }

        private bool ignoreNextMouseMove;

        private void DragMoveWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed) return;
            if (e.RightButton == MouseButtonState.Pressed) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            if (WindowState == WindowState.Maximized && e.ClickCount != 2) return;

            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                ignoreNextMouseMove = true;
                return;
            }

            DragMove();
        }

        private void ShellViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            //            if (DocumentIsOpen && !Header.IsMouseOver) return;
            ToggleMaximized();
            ignoreNextMouseMove = true;
        }

        private void ToggleMaximized()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        // when maximized snap it out by dragging the title bar
        private void MouseMoveWindow(object sender, MouseEventArgs e)
        {
            var mouseY = Mouse.GetPosition(this).Y;
            Console.WriteLine("mouse getPos is at:" + mouseY + " WHILE event is " + e.GetPosition(this).Y);

            //            if (WindowState == WindowState.Maximized && e.GetPosition(this).Y <= 30 && !ShowTitleBar) ShowTitleBar = true;
            //            if (WindowState == WindowState.Maximized && e.GetPosition(this).Y > 30 && ShowTitleBar) ShowTitleBar = false;

            if (ignoreNextMouseMove)
            {
                ignoreNextMouseMove = false;
                return;
            }

            if (WindowState != WindowState.Maximized) return;

            if (e.MiddleButton == MouseButtonState.Pressed) return;
            if (e.RightButton == MouseButtonState.Pressed) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            //            var mouseY = Mouse.GetPosition(this).Y;
            Console.WriteLine("mouse getPos is at:" + mouseY + " WHILE event is " + e.GetPosition(this).Y);
            if (mouseY > TitlebarHeight) return; // don't snap out if we're over the height of the title bar

            // Calculate correct left coordinate for multi-screen system
            var mouseX = PointToScreen(Mouse.GetPosition(this)).X;
            var width = RestoreBounds.Width;
            var left = mouseX - width / 2;
            if (left < 0) left = 0;

            // Align left edge to fit the screen
            var virtualScreenWidth = SystemParameters.VirtualScreenWidth;
            if (left + width > virtualScreenWidth) left = virtualScreenWidth - width;

            Top = 0;
            Left = left;

            WindowState = WindowState.Normal;

            DragMove();
        }
    }
}