using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace LeStreamsFace
{
    internal static class Extensions
    {
        // programmatic version of default(Type)
        public static object GetDefault(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            return source.Contains(toCheck, StringComparison.OrdinalIgnoreCase);
        }

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        public static void DelayInvoke(this Dispatcher dispatcher, TimeSpan ts, Action action)
        {
            DispatcherTimer delayTimer = new DispatcherTimer(DispatcherPriority.Send, dispatcher);
            delayTimer.Interval = ts;
            delayTimer.Tick += (s, e) =>
            {
                delayTimer.Stop();
                action();
            };
            delayTimer.Start();
        }
    }
}