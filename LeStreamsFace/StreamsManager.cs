using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LeStreamsFace
{
    internal enum FiltersEnum
    {
        [Description("League of Legends")]
        League,

        [Description("StarCraft II")]
        Starcraft,

        [Description("Diablo III")]
        Diablo,

        [Description("Dota 2")]
        Dota
    }

    internal static class StreamsManager
    {
        public static readonly ObservableCollection<Stream> Streams = new ObservableCollection<Stream>();
        public static readonly Dictionary<FiltersEnum, bool?> Filters = new Dictionary<FiltersEnum, bool?>();

        static StreamsManager()
        {
            foreach (int enumInt in Enum.GetValues(typeof(FiltersEnum)))
            {
                Filters.Add((FiltersEnum)enumInt, null);
            }
        }
    }
}