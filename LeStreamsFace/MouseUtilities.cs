using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace LeStreamsFace
{
    internal static class MouseUtilities
    {
        public static Point CorrectGetPosition(Visual relativeTo)
        {
            Win32Point w32Mouse = new Win32Point();
            NativeMethods.GetCursorPos(ref w32Mouse);
            return relativeTo.PointFromScreen(new Point(w32Mouse.X, w32Mouse.Y));
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };
    }
}