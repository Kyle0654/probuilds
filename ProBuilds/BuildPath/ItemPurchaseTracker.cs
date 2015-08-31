using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ProBuilds.Match;
using RiotSharp.MatchEndpoint;

namespace ProBuilds.BuildPath
{
    public class ItemPurchaseTrackerData
    {
        public int ItemId;

        public int Number;
        public long Count;
        public double AveragePurchaseTimeSeconds;

        private long Kills;
        private long TowerKills;
        private long InnerTowerKills;
        private long BaseTowerKills;

        public long AverageKills { get { return Kills / Count; } }
        public long AverageTowerKills { get { return TowerKills / Count; } }
        public long AverageInnerTowerKills { get { return InnerTowerKills / Count; } }
        public long AverageBaseTowerKills { get { return BaseTowerKills / Count; } }

        /// <summary>
        /// How many times this item was built into other items.
        /// </summary>
        public Dictionary<int, long> BuiltInto = new Dictionary<int, long>();

        /// <summary>
        /// How many times this item was eventually built into a final item.
        /// </summary>
        public Dictionary<int, long> FinalBuildItem = new Dictionary<int, long>();

        public ItemPurchaseTrackerData(int itemId) { ItemId = itemId; }

        public ItemPurchaseTrackerData(int itemId, int number, ItemPurchaseInformation purchase)
        {
            ItemId = itemId;
            Number = number;
            Count = 1;
            AveragePurchaseTimeSeconds = purchase.GameState.Timestamp.TotalSeconds;
            Kills = purchase.GameState.TotalKills;
            TowerKills = purchase.GameState.TotalTowerKills;
            InnerTowerKills = purchase.GameState.TotalTowerKillsByType(TowerType.InnerTurret);
            BaseTowerKills = purchase.GameState.TotalTowerKillsByType(TowerType.BaseTurret);

            if (purchase.BuildsInto != null)
                BuiltInto.Add(purchase.BuildsInto.ItemId, 1);

            var finalItem = purchase.FinalBuildItem;
            if (finalItem != null)
                FinalBuildItem.Add(finalItem.ItemId, 1);
        }

        public void Increment(ItemPurchaseInformation purchase)
        {
            double lerpValue = (double)Count / (double)(Count + 1);
            AveragePurchaseTimeSeconds = (AveragePurchaseTimeSeconds * lerpValue) + (purchase.GameState.Timestamp.TotalSeconds * (1.0 - lerpValue));
            ++Count;
            Kills += purchase.GameState.TotalKills;
            TowerKills += purchase.GameState.TotalTowerKills;
            InnerTowerKills += purchase.GameState.TotalTowerKillsByType(TowerType.InnerTurret);
            BaseTowerKills += purchase.GameState.TotalTowerKillsByType(TowerType.BaseTurret);

            if (purchase.BuildsInto != null)
            {
                if (BuiltInto.ContainsKey(purchase.BuildsIntoItemId))
                {
                    ++BuiltInto[purchase.BuildsIntoItemId];
                }
                else
                {
                    BuiltInto[purchase.BuildsIntoItemId] = 1;
                }
            }

            var finalItem = purchase.FinalBuildItem;
            if (finalItem != null)
            {
                if (FinalBuildItem.ContainsKey(finalItem.ItemId))
                {
                    ++FinalBuildItem[finalItem.ItemId];
                }
                else
                {
                    FinalBuildItem[finalItem.ItemId] = 1;
                }
            }
        }

        protected void CopyFrom(ItemPurchaseTrackerData other)
        {
            ItemId = other.ItemId;
            Number = other.Number;
            Count = other.Count;
            AveragePurchaseTimeSeconds = other.AveragePurchaseTimeSeconds;
            Kills = other.Kills;
            TowerKills = other.TowerKills;
            InnerTowerKills = other.InnerTowerKills;
            BaseTowerKills = other.BaseTowerKills;
        }

        public ItemPurchaseTrackerData Clone()
        {
            var tracker = new ItemPurchaseTrackerData(this.ItemId);
            tracker.CopyFrom(this);
            return tracker;
        }

        public void Combine(ItemPurchaseTrackerData other)
        {
            if (other.ItemId != ItemId ||
                other.Number != Number)
                return;

            double totalCount = Count + other.Count;
            
            AveragePurchaseTimeSeconds =
                (AveragePurchaseTimeSeconds * ((double)Count / totalCount)) +
                (other.AveragePurchaseTimeSeconds * ((double)other.Count / totalCount));

            Count += other.Count;

            Kills += other.Kills;
            TowerKills += other.TowerKills;
            InnerTowerKills += other.InnerTowerKills;
            BaseTowerKills += other.BaseTowerKills;
        }
    }

    /// <summary>
    /// Keep track of how many times an item was bought on a champion.
    /// </summary>
    public class ItemPurchaseTracker
    {
        public int ItemId;

        /// <summary>
        /// Which number of purchase this was for this item.
        /// e.g. if you purchase 3 wards in a game, you'd have 3 entries here, so we could say you purchase 3 wards 10% of the time
        /// </summary>
        public ConcurrentDictionary<int, ItemPurchaseTrackerData> PerMatchCounts = new ConcurrentDictionary<int, ItemPurchaseTrackerData>();

        public ItemPurchaseTracker(int itemId)
        {
            ItemId = itemId;
        }

        /// <summary>
        /// Increments the count of number of times an item was purchased "number" times in a match, with average purchase time.
        /// </summary>
        public void Increment(int number, ItemPurchaseInformation purchase)
        {
            PerMatchCounts.AddOrUpdate(number,
                id => new ItemPurchaseTrackerData(ItemId, id, purchase),
                (id, tracker) => { tracker.Increment(purchase); return tracker; }
            );
        }

        public static ItemPurchaseTracker Combine(ItemPurchaseTracker a, ItemPurchaseTracker b)
        {
            ItemPurchaseTracker tracker = new ItemPurchaseTracker(a.ItemId);
            lock (a.PerMatchCounts)
            {
                lock (b.PerMatchCounts)
                {
                    a.PerMatchCounts.AsParallel().ForAll(kvp => tracker.PerMatchCounts.TryAdd(kvp.Key, kvp.Value.Clone()));
                    b.PerMatchCounts.AsParallel().ForAll(kvp =>
                        tracker.PerMatchCounts.AddOrUpdate(kvp.Key,
                            key => kvp.Value.Clone(),
                            (key, value) => { value.Combine(kvp.Value); return value; }
                        )
                    );
                }
            }
            return tracker;
        }

        public ItemPurchaseTracker Combine(ItemPurchaseTracker other)
        {
            return ItemPurchaseTracker.Combine(this, other);
        }

        public ItemPurchaseTracker Clone()
        {
            ItemPurchaseTracker tracker = new ItemPurchaseTracker(ItemId);
            PerMatchCounts.AsParallel().ForAll(kvp => tracker.PerMatchCounts.TryAdd(kvp.Key, kvp.Value.Clone()));
            return tracker;
        }
    }
}