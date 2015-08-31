using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProBuilds.Match;
using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProBuilds.BuildPath
{
    /// <summary>
    /// Tracks purchases for a champion across multiple matches.
    /// </summary>
    public class ChampionPurchaseTracker
    {
        public int ChampionId { get; private set; }

        /// <summary>
        /// Purchase data sorted by important game data.
        /// </summary>
        public ConcurrentDictionary<PurchaseSetKey, PurchaseSet> PurchaseSets = new ConcurrentDictionary<PurchaseSetKey, PurchaseSet>();

        public ChampionPurchaseTracker(int championId)
        {
            ChampionId = championId;
        }

        /// <summary>
        /// Track purchases for this champion.
        /// </summary>
        public void Process(ChampionMatchItemPurchases matchPurchases)
        {
            // Create or get the purchase set for this match
            PurchaseSetKey key = new PurchaseSetKey(matchPurchases);
            PurchaseSet set = PurchaseSets.GetOrAdd(key, k => new PurchaseSet(k));
            set.Process(matchPurchases);
        }

        /// <summary>
        /// Get stats for this champion's purchases.
        /// </summary>
        public Dictionary<PurchaseSetKey, ChampionPurchaseCalculator> GetStats()
        {
            return PurchaseSets.ToDictionary(
                kvp => kvp.Key,
                kvp => new ChampionPurchaseCalculator(kvp.Value));
        }
    }
}
