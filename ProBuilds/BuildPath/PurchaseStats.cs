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
        /// Item id and stats for the number of times that item was bought.
        /// </summary>
        /// <example>
        /// Items[3012][2].Percentage might return 0.2, meaning that in 20% of games, this champion buys a second item of this type during this stage.
        /// </example>
        public Dictionary<int, Dictionary<int, ItemPurchaseStats>> Items;

        public PurchaseStats(IEnumerable<ItemPurchaseTrackerData> purchases, long matchCount)
        {
            Items = purchases.GroupBy(tracker => tracker.ItemId).ToDictionary(
                g => g.Key,
                g => g.ToDictionary(tracker => tracker.Number, tracker => new ItemPurchaseStats(tracker, matchCount))
            );
        }
    }
}