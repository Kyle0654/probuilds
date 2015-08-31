using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ProBuilds.BuildPath
{
    /// <summary>
    /// Purchase stats for all items at some stage of the game.
    /// </summary>
    public class PurchaseStats
    {
        /// <summary>
        /// Create item stats, keyed first by item id, then by number of times that item was bought.
        /// </summary>
        /// <example>
        /// Items[3012][2].Percentage might return 0.2, meaning that in 20% of games, this champion buys a second item of this type.
        /// </example>
        public static Dictionary<int, Dictionary<int, ItemPurchaseStats>> Create(IEnumerable<ItemPurchaseTrackerData> purchases, long matchCount)
        {
            return purchases.GroupBy(tracker => tracker.ItemId).ToDictionary(
                g => g.Key,
                g => g.ToDictionary(tracker => tracker.Number, tracker => new ItemPurchaseStats(tracker, matchCount))
            );
        }
    }
}