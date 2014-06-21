using MahApps.Metro.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        private Func<bool> timeBlockCheck;
        private StreamsListViewModel vm;

        public StreamsListWindow(Func<bool> timeBlockCheck)
        {
            InitializeComponent();
            DataContext = vm = new StreamsListViewModel(this);

            this.timeBlockCheck = timeBlockCheck;

            NameScope.SetNameScope(windowCommands, NameScope.GetNameScope(this));

            TypeDescriptor.GetProperties(this.streamsDataGrid)["ItemsSource"].AddValueChanged(this.streamsDataGrid, new EventHandler(blockedItemsListBox_ItemsSourceChanged));
            UpdateGameIconBackgrounds();
            this.streamsDataGrid.ItemsSource = StreamsManager.Streams;

            if (ConfigManager.Instance.SaveWindowPosition)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
            }
            else
            {
                Width = 1000;
                Height = 580;
            }

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
                    view.Filter = vm.Filter;

                    if (view.CanSort)
                    {
                        view.SortDescriptions.Clear();
                        view.SortDescriptions.Add(new SortDescription("IsFavorite", ListSortDirection.Descending));
                        view.SortDescriptions.Add(new SortDescription("Viewers", ListSortDirection.Descending));
                    }
                }
            }
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
                    vm.GameFilteringByIconsEnabled = !vm.GameFilteringByIconsEnabled;

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
            vm.GameFilteringByIconsEnabled = true;

            FiltersEnum myFiltersEnum = (FiltersEnum)((Button)sender).Tag;
            var filteringForGame = StreamsManager.Filters[myFiltersEnum];

            if (filteringForGame == null)
            {
                filteringForGame = true;
            }
            else if (filteringForGame == true)
            {
                filteringForGame = false;
            }
            else if (filteringForGame == false)
            {
                filteringForGame = null;
            }
            StreamsManager.Filters[myFiltersEnum] = filteringForGame;

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

        private void window_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!streamsTabItem.IsSelected || Flyouts.GetChildObjects().Cast<Flyout>().Any(flyout => flyout.IsOpen))
            {
                return;
            }
            if (e.Key == Key.Up || e.Key == Key.Down) // for keyboard streams list navigation
            {
                return;
            }

            if (e.Key == Key.Escape || e.Key == Key.Back && string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                streamsDataGrid.Focus();
                searchTextBox.Text = string.Empty;
                searchPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                searchPanel.Visibility = Visibility.Visible;
                searchTextBox.Focus();
            }
        }

        private void window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!streamsTabItem.IsSelected)
            {
                return;
            }
        }

        private void window_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = searchTextBox.Text;
            if (!string.IsNullOrWhiteSpace(text))
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

        private void DragMoveWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed) return;
            if (e.RightButton == MouseButtonState.Pressed) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            if (WindowState == WindowState.Maximized && e.ClickCount != 2) return;

            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                return;
            }

            DragMove();
        }

        private void ShellViewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ToggleMaximized();
        }

        private void ToggleMaximized()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void GamesPanel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gamesPanel.SelectedIndex = -1;
        }

        private void SelectedGamesPanel_ItemsSourceUpdated(object sender, DataTransferEventArgs e)
        {
            var notifyCollectionChanged = (selectedGamesPanel.ItemsSource) as INotifyCollectionChanged;
            notifyCollectionChanged.CollectionChanged += new NotifyCollectionChangedEventHandler(SelectedGamesPanel_CollectionChanged);
        }

        private void SelectedGamesPanel_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            var itemsSource = selectedGamesPanel.ItemsSource as IEnumerable<Stream>;
            if (itemsSource == null || !itemsSource.Any()) return;

            selectedGamesPanel.ScrollIntoView(itemsSource.First());
        }
    }
}