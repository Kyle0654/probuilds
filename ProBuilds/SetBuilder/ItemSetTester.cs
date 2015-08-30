using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ProBuilds.SetBuilder
{
    /// <summary>
    /// Structure that holds everything in an item set to write out to JSON
    /// </summary>
    static class ItemSetTester
    {
        private static ItemSet createTestSet(string championKey = "Ashe")
        {
            ItemSet set = new ItemSet();

            set.title = championKey + " ProBuilds";
            set.description = "this is a test.";
            set.type = ItemSet.Type.Custom;
            set.map = ItemSet.Map.SummonersRift;
            set.mode = ItemSet.Mode.any;
            set.priority = false;
            set.sortrank = 0;
            set.blocks = new List<ItemSet.Block>();
            ItemSet.Block block = new ItemSet.Block();
            block.type = "Starting Out";
            block.recMath = true;
            block.minSummonerLevel = -1;
            block.maxSummonerlevel = -1;
            block.showIfSummonerSpell = "";
            block.hideIfSummonerSpell = "";
            block.items = new List<ItemSet.Item>();
            ItemSet.Item item = new ItemSet.Item();
            item.id = "1001";
            item.count = 2;
            item.percentage = 0.54f;
            block.items.Add(item);
            set.blocks.Add(block);

            return set;
        }

        public static void testWrite()
        {
            string championKey = "Ashe";
            ItemSet set = createTestSet(championKey);
            bool success = ItemSetWriter.writeOutItemSet(set, championKey, "Boots");

            Console.WriteLine(success ? "Wrote out set" : "Couldn't write set (Already exists?)");
        }

        public static void testReader()
        {
            string championKey = "Ashe";
            ItemSet.Map map = ItemSet.Map.SummonersRift;
            ItemSet set = ItemSetWriter.readInItemSet(championKey, map, "Boots");

            if (set != null)
            {
                string JSON = JsonConvert.SerializeObject(set, Formatting.Indented);
                Console.Write(JSON);
            }
            else
            {
                Console.WriteLine("No item set found");
            }
        }

        //public static void testGenerator(Dictionary<int, ChampionPurchaseStats> championPurchaseStats)
        //{
        //    //Dictionary<int, ItemSet> itemSets = new Dictionary<int, ItemSet>();
        //    //ItemSetGenerator.generateAll(championPurchaseStats, 0.5f, out itemSets);
        //    ChampionStatic champInfo;
        //    if (StaticDataStore.Champions.Champions.TryGetValue("Ashe", out champInfo))
        //    {
        //        ChampionPurchaseStats champStats;
        //        if (championPurchaseStats.TryGetValue(champInfo.Id, out champStats))
        //        {
        //            ItemSet itemSet;
        //            ItemSetGenerator.generate(champStats.ChampionId, champStats, 0.5f, out itemSet);
        //            ItemSetWriter.writeOutItemSet(itemSet, champInfo.Key);
        //        }
        //    }
        //}
    }
}