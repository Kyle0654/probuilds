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
        /// <summary>
        /// Key for a purchase
        /// </summary>
        public struct ItemPurchaseKey
        {
            public int ItemId;
            public int Number;

            public ItemPurchaseKey(int itemId, int number)
            {
                ItemId = itemId;
                Number = number;
            }

            public ItemPurchaseKey(ItemPurchaseInformation purchase)
            {
                ItemId = purchase.ItemId;
                Number = purchase.Number;
            }

            public ItemPurchaseKey(ItemPurchaseTrackerData tracker)
            {
                ItemId = tracker.ItemId;
                Number = tracker.Number;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ItemPurchaseKey))
                    return false;

                ItemPurchaseKey other = (ItemPurchaseKey)obj;
                return
                    ItemId == other.ItemId &&
                    Number == other.Number;
            }

            public override int GetHashCode()
            {
                return Tuple.Create(ItemId, Number).GetHashCode();
            }

            public static bool operator ==(ItemPurchaseKey a, ItemPurchaseKey b)
            {
               return a.Equals(b);
            }

            public static bool operator !=(ItemPurchaseKey a, ItemPurchaseKey b)
            {
                return !a.Equals(b);
            }

            public override string ToString()
            {
                return string.Format("({0},{1})", ItemId, Number);
            }
        }

        public int ItemId;

        public int Number;
        public long Count;
        public double AveragePurchaseTimeSeconds;

        private long Kills;
        private long TowerKills;
        private long InnerTowerKills;
        private long BaseTowerKills;

        public float AverageKills { get { return (float)Kills / (float)Count; } }
        public float AverageTowerKills { get { return (float)TowerKills / (float)Count; } }
        public float AverageInnerTowerKills { get { return (float)InnerTowerKills / (float)Count; } }
        public float AverageBaseTowerKills { get { return (float)BaseTowerKills / (float)Count; } }

        /// <summary>
        /// How many times this item was built into other items (by the number of the other item that has been bought).
        /// </summary>
        public Dictionary<ItemPurchaseKey, long> BuiltInto = new Dictionary<ItemPurchaseKey, long>();

        /// <summary>
        /// How many times this item was eventually built into a final item (by the number of the other item that has been bought).
        /// </summary>
        public Dictionary<ItemPurchaseKey, long> FinalBuildItem = new Dictionary<ItemPurchaseKey, long>();

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
                BuiltInto.Add(new ItemPurchaseKey(purchase.BuildsInto), 1);

            var finalItem = purchase.FinalBuildItem;
            if (finalItem != null)
                FinalBuildItem.Add(new ItemPurchaseKey(finalItem), 1);
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
                var buildsIntoKey = new ItemPurchaseKey(purchase.BuildsInto);
                if (BuiltInto.ContainsKey(buildsIntoKey))
                {
                    ++BuiltInto[buildsIntoKey];
                }
                else
                {
                    BuiltInto[buildsIntoKey] = 1;
                }
            }

            if (purchase.FinalBuildItem != null)
            {
                var finalBuildKey = new ItemPurchaseKey(purchase.FinalBuildItem);
                if (FinalBuildItem.ContainsKey(finalBuildKey))
                {
                    ++FinalBuildItem[finalBuildKey];
                }
                else
                {
                    FinalBuildItem[finalBuildKey] = 1;
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

            BuiltInto = new Dictionary<ItemPurchaseKey, long>(other.BuiltInto);
            FinalBuildItem = new Dictionary<ItemPurchaseKey, long>(other.FinalBuildItem);
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
        public void Increment(ItemPurchaseInformation purchase)
        {
            PerMatchCounts.AddOrUpdate(purchase.Number,
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