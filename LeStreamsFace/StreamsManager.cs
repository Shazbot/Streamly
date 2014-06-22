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

        [Description("StarCraft II: Heart of the Swarm")]
        Starcraft,

        [Description("Diablo III: Reaper of Souls")]
        Diablo,

        [Description("Dota 2")]
        Dota,

        [Description("Hearthstone: Heroes of Warcraft")]
        Hearthstone
    }

    internal static class StreamsManager
    {
        public static readonly OptimizedObservableCollection<Stream> Streams = new OptimizedObservableCollection<Stream>();
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