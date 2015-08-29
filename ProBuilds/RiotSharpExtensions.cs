using RiotSharp;
using RiotSharp.StaticDataEndpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    /// <summary>
    /// Extensions for RiotSharp
    /// </summary>
    static class RiotSharpExtensions
    {
        public static bool IsRetryable(this RiotSharpException ex)
        {
            return (ex.Message.StartsWith("429") || ex.Message.StartsWith("5"));
        }

        public static ChampionStatic GetChampionById(this ChampionListStatic list, int id)
        {
            string key;
            if (!list.Keys.TryGetValue(id, out key))
                return null;

            ChampionStatic champion;
            if (!list.Champions.TryGetValue(key, out champion))
                return null;

            return champion;
        }
    }
}
