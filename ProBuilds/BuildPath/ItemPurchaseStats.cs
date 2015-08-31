
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
        /// The total number of matches this stat was computed across.
        /// </summary>
        public long TotalMatches;

        public ItemPurchaseStats(ItemPurchaseTrackerData tracker, long totalMatches) : base(tracker.ItemId)
        {
            CopyFrom(tracker);
            Percentage = (float)this.Count / (float)totalMatches;
            TotalMatches = totalMatches;
        }
    }
}