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
        public static Dictionary<PurchaseSetKey, ItemSet> generateAll(Dictionary<int, Dictionary<PurchaseSetKey, ChampionPurchaseCalculator>> championPurchaseStats)
        {
            // Extract all sets (keys include champion id, so we don't really need the outer key)
            var allstats = championPurchaseStats.SelectMany(statDictionary => statDictionary.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Loop through all our champions we have stats for and generate sets for them
            var itemSets = allstats.AsParallel().WithDegreeOfParallelism(4).ToDictionary(
                kvp => kvp.Key,
                kvp => generate(kvp.Key, kvp.Value)
            );

            return itemSets;
        }

        public static ItemSet generate(PurchaseSetKey setKey, ChampionPurchaseCalculator stats)
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
            itemSet.description = "Pro item set generated using " + stats.MatchCount.ToString() + " challenger/master matches.";

            //Fill out a bunch of other stuff that is the same for this tool
            itemSet.type = type;
            itemSet.map = map;
            itemSet.mode = mode;
            itemSet.priority = priority;
            itemSet.sortrank = sortRank < 0 ? 0 : sortRank;

            // Block info
            var blockData = new[]
            {
                new { Name = "Starting Items", Stage = GameStage.Start },
                new { Name = "Early Items", Stage = GameStage.Early },
                new { Name = "Midgame Items", Stage = GameStage.Mid },
                new { Name = "Lategame Items", Stage = GameStage.Late }
            };

            // Create blocks and filter to non-empty blocks
            var blocks = blockData
                .Where(blockInfo => stats.Purchases.ContainsKey(blockInfo.Stage))
                .Select(blockInfo =>
                new ItemSet.Block(blockInfo.Name)
                {
                    items = stats.Purchases[blockInfo.Stage].Select(entry => new ItemSet.Item(entry.ItemId.ToString())
                    {
                        count = 1,
                        percentage = entry.Percentage
                    }).ToList()
                })
                .Where(block => block.items.Count > 0).ToList();

            // Combine adjacent items
            blocks.ForEach(block =>
            {
                for (int i = 0; i < block.items.Count - 1; ++i)
                {
                    if (block.items[i].id == block.items[i + 1].id)
                    {
                        // Remove when adjacent, and keep the lower percentage
                        ++block.items[i + 1].count;
                        block.items.RemoveAt(i);
                        --i;
                    }
                }
            });

            // Add blocks to item set
            itemSet.blocks = blocks;

            return itemSet;
        }
    }
}