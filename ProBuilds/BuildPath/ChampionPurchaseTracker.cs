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
    }

    /// <summary>
    /// Purchase stats for all items at some stage of the game.
    /// </summary>
    public class PurchaseStats
    {
        /// <summary>
        /// Item id and percent of times that item was bought.
        /// </summary>
        /// <example>
        /// Items[3012][2] might return 0.2, meaning that in 20% of games, this champion buys a second item of this type during this stage.
        /// </example>
        public Dictionary<int, List<float>> Items;

        public PurchaseStats(ConcurrentDictionary<int, ItemCountTracker> purchases, int matchCount)
        {
            Items = purchases.ToDictionary(kvp => kvp.Value.ItemId, kvp => kvp.Value.PerMatchCounts.Select(ct => (float)ct / (float)matchCount).ToList());
        }
    }

    /// <summary>
    /// Stats for all states of the game.
    /// </summary>
    public class ChampionPurchaseStats
    {
        public int ChampionId;
        public int MatchCount;

        public PurchaseStats Start;
        public PurchaseStats Early;
        public PurchaseStats Mid;
        public PurchaseStats Late;

        public ChampionPurchaseStats(ChampionPurchaseTracker tracker)
        {
            ChampionId = tracker.ChampionId;
            MatchCount = tracker.MatchCount;
            Start = new PurchaseStats(tracker.StartPurchases, tracker.MatchCount);
            Early = new PurchaseStats(tracker.EarlyPurchases, tracker.MatchCount);
            Mid = new PurchaseStats(tracker.MidPurchases, tracker.MatchCount);
            Late = new PurchaseStats(tracker.LatePurchases, tracker.MatchCount);
        }
    }

    /// <summary>
    /// Tracks purchases for a champion across multiple matches.
    /// </summary>
    public class ChampionPurchaseTracker
    {
        public int ChampionId { get; private set; }

        public ConcurrentDictionary<int, ItemCountTracker> StartPurchases = new ConcurrentDictionary<int, ItemCountTracker>();
        public ConcurrentDictionary<int, ItemCountTracker> EarlyPurchases = new ConcurrentDictionary<int, ItemCountTracker>();
        public ConcurrentDictionary<int, ItemCountTracker> MidPurchases = new ConcurrentDictionary<int, ItemCountTracker>();
        public ConcurrentDictionary<int, ItemCountTracker> LatePurchases = new ConcurrentDictionary<int, ItemCountTracker>();

        /// <summary>
        /// Get stats for this champion's purchases.
        /// </summary>
        public ChampionPurchaseStats GetStats()
        {
            return new ChampionPurchaseStats(this);
        }

        public int MatchCount = 0;

        public ChampionPurchaseTracker(int championId)
        {
            ChampionId = championId;
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
    }
}
