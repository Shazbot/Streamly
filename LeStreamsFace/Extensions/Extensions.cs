using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Threading;

namespace LeStreamsFace
{
    internal static class Extensions
    {
        // programmatic version of default(Type)
        public static object GetDefault(this Type type)
        {
            if (type == null) return null;

            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            if (source == null) return false;

            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            if (source == null) return false;

            return source.Contains(toCheck, StringComparison.OrdinalIgnoreCase);
        }

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            if (enumeration == null) return;

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

        public static string GetVariableName<T>(Expression<Func<T>> expression)
        {
            var body = ((MemberExpression)expression.Body);

            return body.Member.Name;
        }

        // put an extension on PropertyChangedEventHandler
        public static string GetVariableName<T>(this PropertyChangedEventHandler propertyChanged, Expression<Func<T>> expression)
        {
            return GetVariableName(expression);
        }
    }
}