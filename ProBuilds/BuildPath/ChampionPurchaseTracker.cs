using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
    /// Keep track of how many times an item was bought on a champion.
    /// </summary>
    public class ItemCountTracker
    {
        public int ItemId;

        /// <summary>
        /// Which number of purchase this was for this item.
        /// e.g. if you purchase 3 wards in a game, you'd have 3 entries here, so we could say you purchase 3 wards 10% of the time
        /// </summary>
        public List<int> PerMatchCounts = new List<int>();

        public ItemCountTracker(int itemId)
        {
            ItemId = itemId;
        }

        public void Increment(int count)
        {
            // Lock the dictionary - it's easier than a concurrent dictionary with the operations we do here
            lock (PerMatchCounts)
            {
                for (int i = 0; i < count; ++i)
                {
                    if (PerMatchCounts.Count <= i)
                    {
                        PerMatchCounts.Add(1);
                    }
                    else
                    {
                        PerMatchCounts[i] = PerMatchCounts[i] + 1;
                    }
                }
            }
        }

        public static ItemCountTracker Combine(ItemCountTracker a, ItemCountTracker b)
        {
            ItemCountTracker tracker = new ItemCountTracker(a.ItemId);
            lock (a.PerMatchCounts)
            {
                lock (b.PerMatchCounts)
                {
                    int count = Math.Max(a.PerMatchCounts.Count, b.PerMatchCounts.Count);
                    for (int i = 0; i < count; ++i)
                    {
                        tracker.PerMatchCounts.Add(
                            (a.PerMatchCounts.Count > i ? a.PerMatchCounts[i] : 0) +
                            (b.PerMatchCounts.Count > i ? b.PerMatchCounts[i] : 0));
                    }
                }
            }
            return tracker;
        }

        public ItemCountTracker Combine(ItemCountTracker other)
        {
            return ItemCountTracker.Combine(this, other);
        }

        public ItemCountTracker Clone()
        {
            ItemCountTracker tracker = new ItemCountTracker(ItemId);
            lock (PerMatchCounts)
            {
                tracker.PerMatchCounts.AddRange(PerMatchCounts);
            }
            return tracker;
        }
    }

    /// <summary>
    /// Purchase stats for an item.
    /// </summary>
    public class ItemPurchaseStats
    {
        /// <summary>
        /// The percent of times this item was purchased
        /// </summary>
        public float Percentage;
    }

    /// <summary>
    /// Purchase stats for all items at some stage of the game.
    /// </summary>
    public class PurchaseStats
    {
        /// <summary>
        /// Item id and stats for the number of times that item was bought.
        /// </summary>
        /// <example>
        /// Items[3012][2].Percentage might return 0.2, meaning that in 20% of games, this champion buys a second item of this type during this stage.
        /// </example>
        public Dictionary<int, List<ItemPurchaseStats>> Items;

        public PurchaseStats(ConcurrentDictionary<int, ItemCountTracker> purchases, long matchCount)
        {
            Items = purchases.ToDictionary(
                kvp => kvp.Value.ItemId,
                kvp => kvp.Value.PerMatchCounts.Select(ct => new ItemPurchaseStats()
                {
                    Percentage = (float)ct / (float)matchCount
                }).ToList()
            );
        }
    }

    /// <summary>
    /// Stats for all states of the game.
    /// </summary>
    public class ChampionPurchaseStats
    {
        public PurchaseSetKey Key;

        public int ChampionId { get { return Key.ChampionId; } }
        public long MatchCount;

        public PurchaseStats Start;
        public PurchaseStats Early;
        public PurchaseStats Mid;
        public PurchaseStats Late;

        public ChampionPurchaseStats(PurchaseSet set)
        {
            Key = set.Key;

            MatchCount = set.MatchCount;
            Start = new PurchaseStats(set.StartPurchases, set.MatchCount);
            Early = new PurchaseStats(set.EarlyPurchases, set.MatchCount);
            Mid = new PurchaseStats(set.MidPurchases, set.MatchCount);
            Late = new PurchaseStats(set.LatePurchases, set.MatchCount);
        }
    }

    /// <summary>
    /// Key for a purchase set.
    /// </summary>
    /// <remarks>This key contains data that should significantly differentiate item purchases throughout a match.</remarks>
    public class PurchaseSetKey
    {
        public int ChampionId { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Lane Lane { get; private set; }
        public bool HasSmite { get; private set; }

        public PurchaseSetKey(ChampionMatchItemPurchases matchPurchases)
        {
            ChampionId = matchPurchases.ChampionId;
            Lane = matchPurchases.Lane;
            HasSmite = matchPurchases.HasSmite;

            // Convert all "Bot" to "Bottom"
            if (Lane == RiotSharp.MatchEndpoint.Lane.Bot)
                Lane = RiotSharp.MatchEndpoint.Lane.Bottom;
        }

        #region Equality

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj == null)
                return true;

            if (!(obj is PurchaseSetKey))
                return false;

            PurchaseSetKey other = obj as PurchaseSetKey;

            return
                ChampionId == other.ChampionId &&
                Lane == other.Lane &&
                HasSmite == other.HasSmite;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(ChampionId, Lane, HasSmite).GetHashCode();
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0} {1} {2}",
                StaticDataStore.Champions != null ?
                    StaticDataStore.Champions.Champions[StaticDataStore.Champions.Keys[ChampionId]].Name :
                    ChampionId.ToString(),
                Lane.ToString(),
                HasSmite ? "smite" : "no-smite");
        }
    }

    /// <summary>
    /// A set of purchases throughout a game.
    /// </summary>
    public class PurchaseSet
    {
        public PurchaseSetKey Key { get; private set; }

        public long MatchCount = 0;

        public ConcurrentDictionary<int, ItemCountTracker> StartPurchases = new ConcurrentDictionary<int, ItemCountTracker>();
        public ConcurrentDictionary<int, ItemCountTracker> EarlyPurchases = new ConcurrentDictionary<int, ItemCountTracker>();
        public ConcurrentDictionary<int, ItemCountTracker> MidPurchases = new ConcurrentDictionary<int, ItemCountTracker>();
        public ConcurrentDictionary<int, ItemCountTracker> LatePurchases = new ConcurrentDictionary<int, ItemCountTracker>();

        public PurchaseSet(PurchaseSetKey key)
        {
            Key = key;
        }

        public void Process(ChampionMatchItemPurchases matchPurchases)
        {
            // Eliminate all undos
            Stack<ItemPurchaseInformation> purchaseStack = new Stack<ItemPurchaseInformation>();

            try
            {
                matchPurchases.ItemPurchases.ForEach(purchase =>
                {
                    if (purchase.EventType != EventType.ItemUndo)
                    {
                        purchaseStack.Push(purchase);
                        return;
                    }
                    else
                    {
                        // Remove any destroy events until the purchase is found
                        while (purchaseStack.Peek().EventType == EventType.ItemDestroyed)
                        {
                            purchaseStack.Pop();
                        }

                        if (purchaseStack.Peek().ItemId != purchase.ItemBefore && purchaseStack.Peek().ItemId != purchase.ItemAfter)
                        {
                            throw new Exception("Undo not matched by purchase or sale.");
                        }

                        purchaseStack.Pop();
                    }
                });
            }
            catch (Exception ex)
            {
                // Something went wrong, this match is invalid
                return;
            }

            List<ItemPurchaseInformation> purchases = purchaseStack.Reverse().ToList();

            // Filter out item purchases by game state
            var startPurchases = purchases.Where(purchase =>
                purchase.GameState.Timestamp < TimeSpan.FromSeconds(90) &&
                purchase.GameState.TotalKills == 0 &&
                purchase.GameState.TotalTowerKills == 0
            ).ToList();

            var earlyPurchases = purchases.Skip(startPurchases.Count).Where(purchase =>
                purchase.GameState.TotalTowerKills == 0
            ).ToList();

            var midPurchases = purchases.Skip(startPurchases.Count + earlyPurchases.Count).Where(purchase =>
                purchase.GameState.TotalTowerKillsByType(TowerType.InnerTurret) < 3 &&
                purchase.GameState.TotalTowerKillsByType(TowerType.BaseTurret) == 0
            ).ToList();

            var latePurchases = purchases.Skip(startPurchases.Count + earlyPurchases.Count + midPurchases.Count).ToList();

            // Determine items bought at stage of game
            var startCounts = startPurchases.Where(purchase => purchase.EventType == EventType.ItemPurchased).GroupBy(purchase => purchase.ItemId).Select(g => new { ItemId = g.Key, Count = g.Count() }).ToList();
            var earlyCounts = earlyPurchases.Where(purchase => purchase.EventType == EventType.ItemPurchased).GroupBy(purchase => purchase.ItemId).Select(g => new { ItemId = g.Key, Count = g.Count() }).ToList();
            var midCounts = midPurchases.Where(purchase => purchase.EventType == EventType.ItemPurchased).GroupBy(purchase => purchase.ItemId).Select(g => new { ItemId = g.Key, Count = g.Count() }).ToList();
            var lateCounts = latePurchases.Where(purchase => purchase.EventType == EventType.ItemPurchased).GroupBy(purchase => purchase.ItemId).Select(g => new { ItemId = g.Key, Count = g.Count() }).ToList();

            // Process into counts
            startCounts.ForEach(p => StartPurchases.GetOrAdd(p.ItemId, id => new ItemCountTracker(id)).Increment(p.Count));
            earlyCounts.ForEach(p => EarlyPurchases.GetOrAdd(p.ItemId, id => new ItemCountTracker(id)).Increment(p.Count));
            midCounts.ForEach(p => MidPurchases.GetOrAdd(p.ItemId, id => new ItemCountTracker(id)).Increment(p.Count));
            lateCounts.ForEach(p => LatePurchases.GetOrAdd(p.ItemId, id => new ItemCountTracker(id)).Increment(p.Count));

            // Increment match processed count
            Interlocked.Increment(ref MatchCount);
        }

        #region Combination

        private static void Combine(ConcurrentDictionary<int, ItemCountTracker> a, ConcurrentDictionary<int, ItemCountTracker> b, ref ConcurrentDictionary<int, ItemCountTracker> set)
        {
            foreach (int key in a.Keys.Union(b.Keys))
            {
                ItemCountTracker at, bt;
                bool ina = a.TryGetValue(key, out at);
                bool inb = b.TryGetValue(key, out bt);
                if (ina && inb)
                {
                    set.TryAdd(key, at.Combine(bt));
                }
                else if (!inb)
                {
                    set.TryAdd(key, at.Clone());
                }
                else if (!ina)
                {
                    set.TryAdd(key, bt.Clone());
                }
            }
        }

        public static PurchaseSet Combine(PurchaseSet a, PurchaseSet b)
        {
            PurchaseSet set = new PurchaseSet(null);
            PurchaseSet.Combine(a.StartPurchases, b.StartPurchases, ref set.StartPurchases);
            PurchaseSet.Combine(a.EarlyPurchases, b.EarlyPurchases, ref set.EarlyPurchases);
            PurchaseSet.Combine(a.MidPurchases, b.MidPurchases, ref set.MidPurchases);
            PurchaseSet.Combine(a.LatePurchases, b.LatePurchases, ref set.LatePurchases);
            return set;
        }

        public PurchaseSet Combine(PurchaseSet other)
        {
            return PurchaseSet.Combine(this, other);
        }

        #endregion
    }

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

        /// <summary>
        /// Get stats for this champion's purchases.
        /// </summary>
        public Dictionary<PurchaseSetKey, ChampionPurchaseStats> GetStats()
        {
            return PurchaseSets.ToDictionary(
                kvp => kvp.Key,
                kvp => new ChampionPurchaseStats(kvp.Value));
        }

        public ChampionPurchaseTracker(int championId)
        {
            ChampionId = championId;
        }

        public void Process(ChampionMatchItemPurchases matchPurchases)
        {
            // Create or get the purchase set for this match
            PurchaseSetKey key = new PurchaseSetKey(matchPurchases);
            PurchaseSet set = PurchaseSets.GetOrAdd(key, k => new PurchaseSet(k));
            set.Process(matchPurchases);
        }
    }
}
