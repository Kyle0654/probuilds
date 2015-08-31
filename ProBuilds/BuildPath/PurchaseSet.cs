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

        public ConcurrentDictionary<int, ItemPurchaseTracker> ItemPurchases = new ConcurrentDictionary<int, ItemPurchaseTracker>();

        /// <summary>
        /// All purchases, including how many of that item were purchased.
        /// </summary>
        public IEnumerable<ItemPurchaseTrackerData> AllItemPurchases
        {
            get { return ItemPurchases.Values.SelectMany(tracker => tracker.PerMatchCounts.Values); }
        }

        public PurchaseSet(PurchaseSetKey key)
        {
            Key = key;
        }

        /// <summary>
        /// Processes purchases to build out information needed to generate an item set.
        /// </summary>
        public void Process(ChampionMatchItemPurchases matchPurchases)
        {
            // Eliminate undo events (and corresponding purchase events)
            var purchases = EliminateUndos(matchPurchases.ItemPurchases);
            if (purchases == null)
                return;

            // Process purchases to determine "builds into" information for each purchase
            AddBuildsIntoInformation(purchases);

            // Use to test that build tree information is correctly generated
            ///////
            //Action<ItemPurchaseInformation> writePurchase = null;
            //writePurchase = new Action<ItemPurchaseInformation>(p =>
            //{
            //    if (p.EventType != EventType.ItemPurchased)
            //        return;

            //    if (p.BuiltFrom.Count != 0)
            //        Console.Write(" => ");

            //    if (p.IsRecipeComponent)
            //        Console.ForegroundColor = ConsoleColor.DarkMagenta;
            //    else if (p.IsDestroyed)
            //        Console.ForegroundColor = ConsoleColor.Red;
            //    else if (p.IsSold)
            //        Console.ForegroundColor = ConsoleColor.Blue;

            //    Console.Write(p.Item.Name);

            //    Console.ResetColor();

            //    if (p.IsRecipeComponent)
            //        writePurchase(p.BuildsInto);
            //    else
            //        Console.WriteLine();
            //});

            //purchases.ForEach(p => writePurchase(p));
            ///////

            // Eliminate sales and destroys (we can get them if needed by following links)
            var filteredPurchases = purchases.Where(purchase => purchase.EventType == EventType.ItemPurchased).ToList();

            // Track purchases
            TrackPurchases(filteredPurchases);

            // Increment match processed count
            Interlocked.Increment(ref MatchCount);
        }

        /// <summary>
        /// Eliminate all undos from a purchase log.
        /// </summary>
        private List<ItemPurchaseInformation> EliminateUndos(List<ItemPurchaseInformation> purchases)
        {
            // Eliminate all undos
            Stack<ItemPurchaseInformation> purchaseStack = new Stack<ItemPurchaseInformation>();

            try
            {
                purchases.ForEach(purchase =>
                {
                    if (purchase.EventType != EventType.ItemUndo)
                    {
                        purchaseStack.Push(purchase);
                        return;
                    }
                    else
                    {
                        // Handle undo
                        // Remove any destroy events until the purchase is found
                        while (purchaseStack.Peek().EventType == EventType.ItemDestroyed)
                        {
                            purchaseStack.Pop();
                        }

                        // Make sure the purchase (or sale) matches the undo
                        if (purchaseStack.Peek().ItemId != purchase.ItemBefore && purchaseStack.Peek().ItemId != purchase.ItemAfter)
                        {
                            throw new Exception("Undo not matched by purchase or sale.");
                        }

                        // Remove the purchase (or sale)
                        purchaseStack.Pop();
                    }
                });
            }
            catch (Exception ex)
            {
                // Something went wrong, this match is invalid
                return null;
            }

            List<ItemPurchaseInformation> ret = purchaseStack.Reverse().ToList();
            return ret;
        }

        /// <summary>
        /// Add information about how items built into other items.
        /// </summary>
        private void AddBuildsIntoInformation(List<ItemPurchaseInformation> purchases)
        {
            ItemPurchaseInformation latestPurchase = null;

            for (int i = 0; i < purchases.Count; ++i)
            {
                ItemPurchaseInformation evt = purchases[i];
                switch (evt.EventType)
                {
                    case EventType.ItemDestroyed:
                        {
                            // Find the item that was destroyed
                            var originalpurchase = purchases.Take(i).LastOrDefault(p => p.ItemId == evt.ItemId && p.IsInInventory);

                            // Check if this item builds into the latest purchase
                            var item = evt.Item;
                            var latestPurchaseItem = (latestPurchase == null) ? null : latestPurchase.Item;

                            if (originalpurchase != null && // some items are granted without a purchase, and can be upgraded (e.g. Viktor's Hex Core)
                                latestPurchase != null &&
                                item.BuildsInto(latestPurchaseItem))
                            {
                                // It does, mark the original purchase as building into the latest purchase
                                originalpurchase.BuildsInto = latestPurchase;
                                latestPurchase.BuiltFrom.Add(originalpurchase);
                            }

                            // Mark the destroy and the purchase it links to
                            if (originalpurchase != null) // some items are granted without a purchase, and can be destroyed (e.g. Kalista's Black Spear)
                            {
                                originalpurchase.DestroyedBy = evt;
                                evt.Destroys = originalpurchase;
                            }
                        }
                        break;
                    case EventType.ItemPurchased:
                        {
                            // Store the new purchase
                            latestPurchase = evt;
                        }
                        break;
                    case EventType.ItemSold:
                        {
                            // Find the purchase to attribute this sale to
                            var salepurchase = purchases.Take(i).LastOrDefault(p => p.ItemId == evt.ItemId && p.IsInInventory);
                            if (salepurchase != null)
                            {
                                salepurchase.SoldBy = evt;
                                evt.Sells = salepurchase;
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Track purchases to generate set-generation information.
        /// </summary>
        private void TrackPurchases(List<ItemPurchaseInformation> purchases)
        {
            Dictionary<int, int> itemPurchaseCounts = new Dictionary<int, int>();
            purchases.ForEach(purchase =>
            {
                if (purchase.EventType != EventType.ItemPurchased)
                    return;

                int count;
                if (!itemPurchaseCounts.TryGetValue(purchase.ItemId, out count))
                {
                    count = 0;
                }

                ++count;
                itemPurchaseCounts[purchase.ItemId] = count;

                ItemPurchases.AddOrUpdate(purchase.ItemId,
                    id =>
                    {
                        var tracker = new ItemPurchaseTracker(id);
                        tracker.Increment(count, purchase);
                        return tracker;
                    },
                    (id, tracker) =>
                    {
                        tracker.Increment(count, purchase);
                        return tracker;
                    }
                );
            });
        }

        #region Combination

        private static void Combine(ConcurrentDictionary<int, ItemPurchaseTracker> a, ConcurrentDictionary<int, ItemPurchaseTracker> b, ref ConcurrentDictionary<int, ItemPurchaseTracker> set)
        {
            foreach (int key in a.Keys.Union(b.Keys))
            {
                ItemPurchaseTracker at, bt;
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
            PurchaseSet.Combine(a.ItemPurchases, b.ItemPurchases, ref set.ItemPurchases);
            return set;
        }

        public PurchaseSet Combine(PurchaseSet other)
        {
            return PurchaseSet.Combine(this, other);
        }

        #endregion
    }
}