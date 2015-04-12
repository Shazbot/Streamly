using Caliburn.Micro;
using MahApps.Metro.Controls;
using System;
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
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace LeStreamsFace
{
    /// <summary>
    /// Interaction logic for BlockedItemsWindow.xaml
    /// </summary>
    internal partial class StreamsListWindow : MetroWindow, IHandle<MinimizeMaximizeMessage>
    {
        public static bool IsMaximized;

        private IEventAggregator _eventAggregator;
        private StreamsListViewModel vm;

        public static bool DoneWithFirstOpening = false;

        public StreamsListWindow(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            DataContext = vm = new StreamsListViewModel(this, eventAggregator);
            vm.OnStreamTabOpening += VmOnOnStreamTabOpening;
            AppLogic.EventAggregator.Subscribe(this);

            _eventAggregator = eventAggregator;

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

            streamsTabItem.IsSelected = true;
        }

        private void window_SourceInitialized(object sender, EventArgs e)
        {
        }

        private void VmOnOnStreamTabOpening(object source, StreamsListViewModel.StreamTabOpeningEventArgs streamTabOpeningEventArgs)
        {
            UnsetGameIconBackground();
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

            _eventAggregator.PublishOnCurrentThread(new TabCreationEvent(sendersStream));
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

        public void SetGameIconBackground()
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
                UnsetGameIconBackground();
            }
        }

        private void streamsDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            SetGameIconBackground();
        }

        private void StreamsDataGrid_OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsetGameIconBackground();
        }

        public void UnsetGameIconBackground()
        {
            AdornerLayer adLayer = AdornerLayer.GetAdornerLayer(streamsDataGrid);
            if (adLayer == null) return;
            adLayer.FindChildren<GameIconAdorner>().ToList().ForEach(adLayer.Remove);
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
            if (!streamsTabItem.IsSelected || vm.IsAnyStreamTabOpen || Flyouts.GetChildObjects().Cast<Flyout>().Any(flyout => flyout.IsOpen))
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
                    _eventAggregator.PublishOnCurrentThread(new TabCreationEvent(selectedStream));
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
            if (vm.IsAnyStreamTabOpen && e.GetPosition(this).Y >= this.ActualHeight - 30) return;

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
            if (vm.IsAnyStreamTabOpen && e.GetPosition(this).Y >= this.ActualHeight - 30) return;

            // look into a per tab way of handling maximize on double click if needed
            //            e.Handled = true;
            //            ToggleMaximized();
        }

        private void ToggleMaximized()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void GamesPanel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gamesPanel.SelectedIndex = -1;
        }

        private void StreamsPanel_ItemsSourceUpdated(object sender, DataTransferEventArgs e)
        {
            var notifyCollectionChanged = (streamsPanel.ItemsSource) as INotifyCollectionChanged;
            if (notifyCollectionChanged == null) return;

            notifyCollectionChanged.CollectionChanged += new NotifyCollectionChangedEventHandler(StreamsPanel_CollectionChanged);
        }

        private void StreamsPanel_CollectionChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            var itemsSource = streamsPanel.ItemsSource as IEnumerable<Stream>;
            if (itemsSource == null || !itemsSource.Any()) return;

            streamsPanel.ScrollIntoView(itemsSource.First());
        }

        private void StreamsListWindow_OnStateChanged(object sender, EventArgs e)
        {
            IsMaximized = WindowState == WindowState.Maximized;

            if (WindowState == WindowState.Maximized)
            {
                ShowTitleBar = false;
            }
            if (WindowState == WindowState.Normal)
            {
                ShowTitleBar = true;
            }
        }

        private void CefWebView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            var stream = ((CefWebView)sender).Tag as Stream;

            if (vm != null)
            {
                if (!vm.RunningStreams.Contains(stream))
                {
                    ((CefWebView)sender).browser.Dispose();
                }
            }
            else
            {
                ((CefWebView)sender).browser.Dispose();
            }
        }

        private void RunningStreamTabMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
            {
                vm.CloseRunningStreamTabCommand.Execute(((FrameworkElement)sender).DataContext);
            }
        }

        private void StreamsListWindow_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                var titleBarOffset = TitlebarHeight * 2;
                if (e.GetPosition(this).Y < TitlebarHeight / 2 && !ShowTitleBar)
                {
                    ShowTitleBar = true;
                }
                else if (e.GetPosition(this).Y >= titleBarOffset && ShowTitleBar)
                {
                    ShowTitleBar = false;
                }
            }
        }

        public void Handle(MinimizeMaximizeMessage message)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void window_OnClosing(object sender, CancelEventArgs e)
        {
            TypeDescriptor.GetProperties(this.streamsDataGrid)["ItemsSource"].RemoveValueChanged(this.streamsDataGrid, new EventHandler(blockedItemsListBox_ItemsSourceChanged));
            vm.CloseAllRunningStreams();
            vm.OnStreamTabOpening -= VmOnOnStreamTabOpening;
        }

        private void window_Closed(object sender, EventArgs e)
        {
            _eventAggregator = null;
            streamsDataGrid.ItemsSource = null;
            DataContext = vm = null;
            gameStreamsPlot.Model = null;
            gameViewersPlot.Model = null;
        }
    }
}