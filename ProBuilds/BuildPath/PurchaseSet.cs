using ProBuilds.Match;
using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProBuilds.BuildPath
{
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
}