using Caliburn.Micro;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Screen = System.Windows.Forms.Screen;

namespace LeStreamsFace
{
    internal partial class NotificationWindow : Window
    {
        private readonly IEventAggregator _eventAggregator;
        private bool removeLater = false;
        private bool animationRunning = true;

        private DispatcherTimer dispatcherTimer;
        private double calculatedTop;

        public NotificationWindow(Stream stream, IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            this.InitializeComponent();
            this.DataContext = stream;

            var point = CalculateTopAndLeft();

            this.Left = point.X;
            this.Top = point.Y;
            calculatedTop = Top;

            var numNotifications = MainWindow.NotificationWindows.Count;
            if (numNotifications > 0)
            {
                this.Top = MainWindow.NotificationWindows[numNotifications - 1].calculatedTop - this.Height;
                calculatedTop = Top;
            }
            MainWindow.NotificationWindows.Add(this);
            this.Opacity = 0.9;
            //            this.Topmost = false;
            this.Show();

            if (ConfigManager.Instance.NotificationTimeout > 0)
            {
                dispatcherTimer = new DispatcherTimer();

                //                Random a = new Random();
                //                dispatcherTimer.Interval = TimeSpan.FromSeconds(a.Next(15) + 5);

                dispatcherTimer.Interval = TimeSpan.FromSeconds(ConfigManager.Instance.NotificationTimeout);
                dispatcherTimer.Tick += DispatcherTimerOnTick;
                dispatcherTimer.Start();
            }
        }

        private void DispatcherTimerOnTick(object sender, EventArgs eventArgs)
        {
            int secsSinceLastInput = NativeMethods.GetLastInputTime();
            if (secsSinceLastInput > 5 * 60)
            {
                return;
            }

            MouseUtilities.Win32Point win32Point = new MouseUtilities.Win32Point();
            NativeMethods.GetCursorPos(ref win32Point);

            foreach (NotificationWindow notificationWindow in MainWindow.NotificationWindows)
            {
                if (VisualTreeHelper.GetDescendantBounds(notificationWindow).Contains(
                    notificationWindow.PointFromScreen(new System.Windows.Point(win32Point.X, win32Point.Y))))
                {
                    return;
                }
            }

            //            this.RemoveNotifications();
            this.Close();
        }

        private Point CalculateTopAndLeft()
        {
            var primaryScreen = Screen.PrimaryScreen;

            var screenHeight = primaryScreen.Bounds.Height;
            var screenWidth = primaryScreen.Bounds.Width;
            if (MainWindow.Taskbar.Position == TaskbarPosition.Bottom && !MainWindow.Taskbar.AutoHide)
            {
                screenHeight = MainWindow.Taskbar.Location.Y;
            }
            else if (MainWindow.Taskbar.Position == TaskbarPosition.Right && !MainWindow.Taskbar.AutoHide)
            {
                screenWidth = MainWindow.Taskbar.Location.X;
            }

            return new Point(screenWidth - this.Width, screenHeight - this.Height);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (animationRunning)
            {
                return;
            }

            MouseUtilities.Win32Point win32Point = new MouseUtilities.Win32Point();
            NativeMethods.GetCursorPos(ref win32Point);

            bool mouseOverAny = false;
            foreach (NotificationWindow notificationWindow in MainWindow.NotificationWindows.Where(window => window != this))
            {
                if (notificationWindow.animationRunning || notificationWindow.endAnimationStarted)
                {
                    continue;
                }

                if (VisualTreeHelper.GetDescendantBounds(notificationWindow).Contains(
                        notificationWindow.PointFromScreen(new System.Windows.Point(win32Point.X, win32Point.Y))))
                {
                    mouseOverAny = true;
                    notificationWindow.removeLater = true;
                }
            }

            this.removeLater = true;

            if (!mouseOverAny)
            {
                this.RemoveNotifications();
            }
        }

        private void RemoveNotifications()
        {
            var index = MainWindow.NotificationWindows.IndexOf(this);
            if (index < 0)
            {
                return;
            }

            this.Close();

            bool allAnimationsDone = MainWindow.NotificationWindows.Where(window => window.endAnimationStarted).All(window => window.endAnimationEnded);

            index = MainWindow.NotificationWindows.IndexOf(this);
            if (index >= 0 && endAnimationEnded)
            {
                MainWindow.NotificationWindows.RemoveAt(index);
            }

            MouseUtilities.Win32Point win32Point = new MouseUtilities.Win32Point();
            NativeMethods.GetCursorPos(ref win32Point);

            // if mouse over any notification don't restack them
            if (MainWindow.NotificationWindows.Where(window => !window.endAnimationStarted && !window.animationRunning).Any(notificationWindow => VisualTreeHelper.GetDescendantBounds(notificationWindow).Contains(
                notificationWindow.PointFromScreen(new System.Windows.Point(win32Point.X, win32Point.Y)))))
            {
                return;
            }

            // remove notifications scheduled for removal
            foreach (NotificationWindow notificationWindow in MainWindow.NotificationWindows.Where(window => window.removeLater))
            {
                if (notificationWindow == this)
                {
                    continue;
                }
                notificationWindow.Close();
            }

            // don't restack if any closing animations running
            if (!allAnimationsDone)
            {
                return;
            }

            // restack all notifications
            foreach (NotificationWindow notificationWindow in MainWindow.NotificationWindows)
            {
                var point = notificationWindow.CalculateTopAndLeft();
                var newTop = point.Y - MainWindow.NotificationWindows.IndexOf(notificationWindow) * notificationWindow.Height;
                notificationWindow.calculatedTop = newTop;

                notificationWindow.BeginAnimation(TopProperty, null);

                notificationWindow.Left = point.X;
                notificationWindow.Top = newTop;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ctrlDown = NativeMethods.IsButtonDown(Keys.ControlKey);
            if (ctrlDown)
            {
                try
                {
                    Clipboard.SetText(((Stream)DataContext).GetUrl());
                }
                catch (Exception)
                {
                }
                return;
            }

            _eventAggregator.PublishOnCurrentThread(new TabCreationEvent((Stream)DataContext));
            //            System.Diagnostics.Process.Start(((Stream)DataContext).GetUrl());
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Opacity = 0.75;
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            //make invisible to task manager
            int exStyle = (int)NativeMethods.GetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            // make click through
            exStyle |= (int)NativeMethods.WS_EX_TRANSPARENT;
            NativeMethods.SetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
            var screenHeight = primaryScreen.Bounds.Height;

            this.WindowTopAnimation.From = screenHeight;
            this.WindowTopAnimation.To = this.calculatedTop;
        }

        private void WindowTopAnimation_Completed(object sender, EventArgs e)
        {
            if (!animationRunning)
            {
                return;
            }

            //            this.Clip = new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            this.IsHitTestVisible = true;
            this.Top = this.calculatedTop;
            animationRunning = false;

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                //make click through
                int exStyle = (int)NativeMethods.GetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE);
                exStyle &= (int)~NativeMethods.WS_EX_TRANSPARENT;
                NativeMethods.SetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
            }
        }

        private bool endAnimationStarted = false;
        private bool endAnimationEnded = false;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!endAnimationEnded)
            {
                e.Cancel = true;
            }

            if (!endAnimationStarted)
            {
                endAnimationStarted = true;
                //                Storyboard sb = this.FindResource("WindowStoryboard2") as Storyboard;

                Storyboard sb = new Storyboard();

                var opacityAnimation = new DoubleAnimation();
                sb.Children.Add(opacityAnimation);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                opacityAnimation.To = 0;
                opacityAnimation.FillBehavior = FillBehavior.HoldEnd;
                opacityAnimation.BeginTime = TimeSpan.FromSeconds(0);
                //                opacityAnimation.Duration = TimeSpan.FromSeconds(0.8);
                opacityAnimation.Duration = TimeSpan.FromSeconds(0.6);

                DoubleAnimation myDoubleAnimation = new DoubleAnimation();
                myDoubleAnimation.To = this.Top + this.Height / 4;

                myDoubleAnimation.BeginTime = TimeSpan.FromSeconds(0);
                //                myDoubleAnimation.Duration = TimeSpan.FromSeconds(0.8);
                myDoubleAnimation.Duration = TimeSpan.FromSeconds(0.6);

                sb.Children.Add(myDoubleAnimation);
                //                Storyboard.SetTargetProperty(myDoubleAnimation, new PropertyPath(System.Windows.Window.LeftProperty));
                Storyboard.SetTargetProperty(myDoubleAnimation, new PropertyPath(System.Windows.Window.TopProperty));

                Timeline.SetDesiredFrameRate(opacityAnimation, 30);
                Timeline.SetDesiredFrameRate(myDoubleAnimation, 30);
                sb.SlipBehavior = SlipBehavior.Slip;

                sb.Completed += (o, args) =>
                {
                    endAnimationEnded = true;

                    MouseUtilities.Win32Point win32Point = new MouseUtilities.Win32Point();
                    NativeMethods.GetCursorPos(ref win32Point);
                    bool mouseOverAny = false;
                    foreach (NotificationWindow notificationWindow in MainWindow.NotificationWindows.Where(window => !window.endAnimationStarted && !window.animationRunning))
                    {
                        if (VisualTreeHelper.GetDescendantBounds(notificationWindow).Contains(
                            notificationWindow.PointFromScreen(new System.Windows.Point(win32Point.X, win32Point.Y))))
                        {
                            mouseOverAny = true;
                            break;
                        }
                    }
                    if (mouseOverAny)
                    {
                        this.removeLater = true;
                    }
                    RemoveNotifications();
                };
                var hwnd = new WindowInteropHelper(this).Handle;
                NativeMethodsTransparency.SetWindowExTransparent(hwnd);
                sb.Begin(this, true);

                //                RemoveNotifications();
            }
        }
    }
}