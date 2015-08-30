using ProBuilds.BuildPath;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProBuilds.SetBuilder
{
    static class ItemSetGenerator
    {
        /// <summary>
        /// Map to set in item sets generated
        /// </summary>
        public static ItemSet.Map map = ItemSet.Map.SummonersRift;
        /// <summary>
        /// Mode to set in item sets generated
        /// </summary>
        public static ItemSet.Mode mode = ItemSet.Mode.any;

        /// <summary>
        /// Type to set in items sets generated
        /// </summary>
        public static ItemSet.Type type = ItemSet.Type.Custom;

        /// <summary>
        /// Priority to use when generating item sets (should this be sorted above all other item sets)
        /// </summary>
        public static bool priority = false;

        /// <summary>
        /// Descending order to sort this item set (anything less than 0 will use the rounded passed min percentage value for this)
        /// </summary>
        public static int sortRank = -1;

        /// <summary>
        /// Generate item sets for all champions (threaded)
        /// </summary>
        /// <param name="championPurchaseStats">Dictionary of champion id's and stats for purchase items for those champions</param>
        /// <returns>True if we were able to generate item sets, false if not</returns>
        public static Dictionary<PurchaseSetKey, ItemSet> generateAll(Dictionary<int, Dictionary<PurchaseSetKey, ChampionPurchaseStats>> championPurchaseStats)
        {
            // Create a dictionary we can use in threading
            System.Collections.Concurrent.ConcurrentDictionary<int, ItemSet> threadedSets = new System.Collections.Concurrent.ConcurrentDictionary<int, ItemSet>();

            // Extract all sets (keys include champion id, so we don't really need the outer key)
            var allstats = championPurchaseStats.SelectMany(statDictionary => statDictionary.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Loop through all our champions we have stats for and generate sets for them
            var itemSets = allstats.AsParallel().WithDegreeOfParallelism(4).ToDictionary(
            kvp => kvp.Key,
            kvp => generate(kvp.Key, kvp.Value)
            );

            return itemSets;
        }

        public static ItemSet generate(PurchaseSetKey setKey, ChampionPurchaseStats stats)
        {
            //Lookup the champion
            var championKey = StaticDataStore.Champions.Keys[setKey.ChampionId];
            var championInfo = StaticDataStore.Champions.Champions[championKey];

            // Create the set and key it
            ItemSet itemSet = new ItemSet();
            itemSet.SetKey = setKey;
            itemSet.MatchCount = stats.MatchCount;

            //Come up with an item set title
            itemSet.title = championInfo.Name + " " + ItemSetNaming.ToolName;

            //Add a nice description of how this was generated
            itemSet.description = "Item set generated using " + stats.MatchCount.ToString() + " matches.";

            //Fill out a bunch of other stuff that is the same for this tool
            itemSet.type = type;
            itemSet.map = map;
            itemSet.mode = mode;
            itemSet.priority = priority;
            itemSet.sortrank = sortRank < 0 ? (int)Math.Round(SetBuilderSettings.ItemMinimumPurchasePercentage.Start * 100) : sortRank;

            // Block info
            var blockData = new[]
{
new { Name = "Starting Items", Items = stats.Start.Items, Min = SetBuilderSettings.ItemMinimumPurchasePercentage.Start },
new { Name = "Early Items", Items = stats.Early.Items, Min = SetBuilderSettings.ItemMinimumPurchasePercentage.Early },
new { Name = "Midgame Items", Items = stats.Mid.Items, Min = SetBuilderSettings.ItemMinimumPurchasePercentage.Mid },
new { Name = "Lategame Items", Items = stats.Late.Items, Min = SetBuilderSettings.ItemMinimumPurchasePercentage.Late }
};

            // Create blocks and filter to non-empty blocks
            var blocks = blockData.Select(blockInfo =>
            new ItemSet.Block(blockInfo.Name)
            {
                items = getItemsWithinMin(blockInfo.Items, blockInfo.Min)
            })
            .Where(block => block.items.Count > 0);

            // Add blocks to item set
            itemSet.blocks = new List<ItemSet.Block>(blocks);

            return itemSet;
        }

        private static List<ItemSet.Item> getItemsWithinMin(Dictionary<int, List<ItemPurchaseStats>> items, float min)
        {
            List<ItemSet.Item> itemsInBlock = items
            .Where(entry => entry.Value.Any(item => item.Percentage >= min))
            .Select(entry => new ItemSet.Item(entry.Key.ToString())
            {
                count = entry.Value.Where(item => item.Percentage >= min).Count(),
                percentage = entry.Value.LastOrDefault(item => item.Percentage >= min).Percentage
            }).ToList();

            return itemsInBlock;

            /*
            //Loop through all the items
            foreach (KeyValuePair<int, List<float>> entry in items)
            {
            //Loop through each item count and percentage
            int count = 0;
            foreach (float percentage in entry.Value)
            {
            count++;

            //Only add the items that are within the percentage
            if (percentage >= min)
            {
            ItemSet.Item item = new ItemSet.Item(entry.Key.ToString());
            item.count = count;
            item.percentage = percentage;
            itemList.Add(item);
            }
            }
            }
            */
        }
    }
}