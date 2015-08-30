using System;
using System.Collections.Generic;

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
}