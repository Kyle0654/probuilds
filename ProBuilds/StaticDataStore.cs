using RiotSharp;
using RiotSharp.StaticDataEndpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public static class StaticDataStore
    {
        public static ChampionListStatic Champions { get; private set; }
        public static ItemListStatic Items { get; private set; }
        public static Realm Realm { get; private set; }

        public static RiotVersion Version { get; private set; }

        /// <summary>
        /// Initialize the static data store by pulling down all data we care about
        /// </summary>
        public static void Initialize(StaticRiotApi riotStaticApi, RiotQuerySettings querySettings)
        {
            // Get static data
            Champions = riotStaticApi.GetChampions(querySettings.Region, ChampionData.all);
            Items = riotStaticApi.GetItems(querySettings.Region, ItemData.all);
            Realm = riotStaticApi.GetRealm(querySettings.Region);

            Version = new RiotVersion(Realm.V);
        }
    }
}
