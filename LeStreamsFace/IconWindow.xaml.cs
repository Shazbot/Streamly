using System;
using System.Windows;
using System.Windows.Interop;

namespace LeStreamsFace
{
    /// <summary>
    /// Interaction logic for IconWindow.xaml
    /// </summary>
    internal partial class IconWindow : Window
    {
        private MainWindow.ExitDelegate exitDelegate;

        internal IconWindow(MainWindow.ExitDelegate exitDelegate)
        {
            InitializeComponent();
            this.exitDelegate = exitDelegate;
        }

        private void OnMenuItemExitClick(object sender, EventArgs e)
        {
            exitDelegate(sender, e);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            //make invisible to task manager
            int exStyle = (int)NativeMethods.GetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)NativeMethods.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(hwnd, (int)NativeMethods.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }
    }
}