using Caliburn.Micro;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

namespace LeStreamsFace
{
    /// <summary>
    /// Interaction logic for IconWindow.xaml
    /// </summary>
    internal partial class IconWindow : Window
    {
        private readonly IEventAggregator _eventAggregator;
        private AppLogic.ExitDelegate exitDelegate;

        internal IconWindow(IEventAggregator eventAggregator, AppLogic.ExitDelegate exitDelegate)
        {
            InitializeComponent();
            _eventAggregator = eventAggregator;
            this.exitDelegate = exitDelegate;
        }

        private void OnMenuItemExitClick(object sender, EventArgs e)
        {
            exitDelegate(sender, e);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // make invisible to task manager
            int exStyle = (int)NativeMethods.GetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            HwndSource src = HwndSource.FromHwnd(hwnd);
            src.AddHook(new HwndSourceHook(WndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == App.WM_SHOWFIRSTINSTANCE)
            {
                _eventAggregator.PublishOnCurrentThread(new OpenStreamsListWindow());
            }
            return IntPtr.Zero;
        }
    }
}