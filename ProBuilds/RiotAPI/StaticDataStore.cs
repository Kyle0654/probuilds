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
    /// All static data we store about a realm.
    /// </summary>
    public class RealmStaticData
    {
        public ChampionListStatic Champions { get; private set; }
        public ItemListStatic Items { get; private set; }
        public SummonerSpellListStatic SummonerSpells { get; private set; }
        public Realm Realm { get; private set; }
        public RiotVersion Version { get; private set; }
        public Region Region { get; private set; }

        internal RealmStaticData(StaticRiotApi riotStaticApi, Realm realm, Region region)
        {
            Realm = realm;
            Version = new RiotVersion(Realm.V);
            Region = region;

            Champions = riotStaticApi.GetChampions(region, ChampionData.all);
            Items = riotStaticApi.GetItems(region, ItemData.all);
            SummonerSpells = riotStaticApi.GetSummonerSpells(region, SummonerSpellData.all);
        }
    }

    public static class StaticDataStore
    {
        /// <summary>
        /// The most current version across all realms.
        /// </summary>
        public static RiotVersion Version { get; private set; }

        /// <summary>
        /// Static data for each realm.
        /// </summary>
        public static Dictionary<Region, RealmStaticData> Realms { get; private set; }

        /// <summary>
        /// The most current champion list from NA.
        /// </summary>
        public static ChampionListStatic Champions { get; private set; }

        /// <summary>
        /// The most current item list from NA.
        /// </summary>
        public static ItemListStatic Items { get; private set; }

        /// <summary>
        /// The most current summoner spell list from NA.
        /// </summary>
        public static SummonerSpellListStatic SummonerSpells { get; private set; }

        /// <summary>
        /// Initialize the static data store by pulling down all data we care about.
        /// </summary>
        public static void Initialize(StaticRiotApi riotStaticApi)
        {
            var realms = Enum.GetValues(typeof(Region)).OfType<Region>().AsParallel().WithDegreeOfParallelism(4).Select(region => new { Region = region, Realm = riotStaticApi.GetRealm(region) }).ToList();
            Version = realms.Max(realm => new RiotVersion(realm.Realm.V));
            var filteredRealms = realms.Where(realm => Version.IsSamePatch(new RiotVersion(realm.Realm.V)));

            // Get data for all valid realms
            Realms = filteredRealms.ToDictionary(realm => realm.Region, realm => new RealmStaticData(riotStaticApi, realm.Realm, realm.Region));

            // Try getting NA data if available
            RealmStaticData primaryrealm;
            if (!Realms.TryGetValue(Region.na, out primaryrealm))
            {
                // Try to find an english realm
                primaryrealm = Realms.FirstOrDefault(kvp => kvp.Value.Realm.L.Contains("en")).Value;
                
                // If we can't find english data, give up and just choose the first realm
                if (primaryrealm == null)
                    primaryrealm = Realms.FirstOrDefault().Value;

                // If there are no realms, just return
                if (primaryrealm == null)
                    return;    
            }

            Champions = primaryrealm.Champions;
            Items = primaryrealm.Items;
            SummonerSpells = primaryrealm.SummonerSpells;
        }
    }
}
