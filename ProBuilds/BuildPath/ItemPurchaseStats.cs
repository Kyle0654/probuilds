using System.Collections.Generic;
using System.Linq;

namespace ProBuilds.BuildPath
{
    /// <summary>
    /// Purchase stats for an item.
    /// </summary>
    public class ItemPurchaseStats : ItemPurchaseTrackerData
    {
        /// <summary>
        /// The percent of times this item was purchased
        /// </summary>
        public float Percentage;

        /// <summary>
        /// The percentage of times this item built into another item, among times this item was built.
        /// </summary>
        public Dictionary<ItemPurchaseKey, float> BuiltIntoPercentage;

        /// <summary>
        /// The percentage of times this item built into another, final item, among times this item was built.
        /// </summary>
        public Dictionary<ItemPurchaseKey, float> FinalBuildItemPercentage;

        /// <summary>
        /// The total number of matches this stat was computed across.
        /// </summary>
        public long TotalMatches;

        public ItemPurchaseStats(ItemPurchaseTrackerData tracker, long totalMatches) : base(tracker.ItemId)
        {
            CopyFrom(tracker);
            Percentage = (float)this.Count / (float)totalMatches;
            TotalMatches = totalMatches;

            // Calculate build path percentages
            BuiltIntoPercentage = BuiltInto.ToDictionary(kvp => kvp.Key, kvp => (float)kvp.Value / (this.Count));
            FinalBuildItemPercentage = FinalBuildItem.ToDictionary(kvp => kvp.Key, kvp => (float)kvp.Value / (this.Count));
        }
    }
}