using Newtonsoft.Json;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.IO;
using ProBuilds.BuildPath;
using RiotSharp.StaticDataEndpoint;
using System.Threading.Tasks;
using System.Linq;

namespace ProBuilds
{
    static class ItemSetNaming
    {
        public const string ToolName = "PBIS";
    }

    /// <summary>
    /// Structure that holds everything in an item set to write out to JSON
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ItemSet
    {
        [JsonObject(MemberSerialization.OptIn)]
        public class Item
        {
            [JsonProperty("id")]
            public string id;

            [JsonProperty("count")]
            public int count;

            public Item(string id = "")
            {
                this.id = id;
                this.count = 0;
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Block
        {
            [JsonProperty("type")]
            public string type;

            [JsonProperty("recMath")]
            public bool recMath;

            [JsonProperty("minSummonerLevel")]
            public int minSummonerLevel;

            [JsonProperty("maxSummonerlevel")]
            public int maxSummonerlevel;

            [JsonProperty("showIfSummonerSpell")]
            public string showIfSummonerSpell;

            [JsonProperty("hideIfSummonerSpell")]
            public string hideIfSummonerSpell;

            [JsonProperty("items")]
            public List<Item> items;

            public Block(string type = "")
            {
                this.type = type;
                this.recMath = false;
                this.minSummonerLevel = -1;
                this.maxSummonerlevel = -1;
                this.showIfSummonerSpell = string.Empty;
                this.hideIfSummonerSpell = string.Empty;
                this.items = null;
            }
        }

        [DataContract]
        public enum Type
        {
            [EnumMember(Value = "custom")]
            Custom,

            [EnumMember(Value = "global")]
            Global
        }

        [DataContract]
        public enum Map
        {
            [EnumMember(Value = "any")]
            any,

            [EnumMember(Value = "SR")]
            SummonersRift,

            [EnumMember(Value = "HA")]
            HowlingAbyss,

            [EnumMember(Value = "TT")]
            TwistedTreeline,

            [EnumMember(Value = "CS")]
            CrystalScar
        }

        [DataContract]
        public enum Mode
        {
            [EnumMember(Value = "any")]
            any,

            [EnumMember(Value = "CLASSIC")]
            Classic,

            [EnumMember(Value = "ARAM")]
            Aram,

            [EnumMember(Value = "ODIN")]
            Dominion
        }

        [JsonIgnore]
        public PurchaseSetKey SetKey;

        [JsonProperty("matchcount")]
        public long MatchCount;

        [JsonProperty("title")]
        public string title;

        [JsonProperty("description")]
        public string description;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Type type;

        [JsonProperty("map")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Map map;

        [JsonProperty("mode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Mode mode;

        [JsonProperty("priority")]
        public bool priority;

        [JsonProperty("sortrank")]
        public int sortrank;

        [JsonProperty("blocks")]
        public List<Block> blocks;

        public ItemSet(string title = "")
        {
            this.SetKey = null;
            this.MatchCount = 0;

            this.title = title;
            this.description = "";
            this.type = Type.Custom;
            this.map = Map.any;
            this.mode = Mode.any;
            this.priority = false;
            this.sortrank = 0;
            this.blocks = null;
        }
    }

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

            if(set != null)
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
        /// <param name="min">Minimum percentage to include the items for</param>
        /// <param name="itemSets">Dictionary to store item sets in, key is champion id</param>
        /// <returns>True if we were able to generate item sets, false if not</returns>
        public static Dictionary<PurchaseSetKey, ItemSet> generateAll(Dictionary<int, Dictionary<PurchaseSetKey, ChampionPurchaseStats>> championPurchaseStats, float min)
        {
            // Create a dictionary we can use in threading
            System.Collections.Concurrent.ConcurrentDictionary<int, ItemSet> threadedSets = new System.Collections.Concurrent.ConcurrentDictionary<int, ItemSet>();

            // Extract all sets (keys include champion id, so we don't really need the outer key)
            var allstats = championPurchaseStats.SelectMany(statDictionary => statDictionary.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Loop through all our champions we have stats for and generate sets for them
            var itemSets = allstats.AsParallel().WithDegreeOfParallelism(4).ToDictionary(
                kvp => kvp.Key,
                kvp => generate(kvp.Key, kvp.Value, min)
            );

            return itemSets;
        }

        public static ItemSet generate(PurchaseSetKey setKey, ChampionPurchaseStats stats, float min)
        {
            //Lookup the champion
            var championKey = StaticDataStore.Champions.Keys[setKey.ChampionId];
            var championInfo = StaticDataStore.Champions.Champions[championKey];

            // Create the set and key it
            ItemSet itemSet = new ItemSet();
            itemSet.SetKey = setKey;
            itemSet.MatchCount = stats.MatchCount;

            //Come up with an item set title
            itemSet.title = championInfo.Name + " " + (min * 100).ToString() + " " + ItemSetNaming.ToolName;

            //Add a nice description of how this was generated
            itemSet.description = "Item set generated using " + stats.MatchCount.ToString() + " matches and only taking items used at least " + (min * 100).ToString() + "% of the time.";

            //Fill out a bunch of other stuff that is the same for this tool
            itemSet.type = type;
            itemSet.map = map;
            itemSet.mode = mode;
            itemSet.priority = priority;
            itemSet.sortrank = sortRank < 0 ? (int)Math.Round(min * 100) : sortRank;

            // Block info
            var blockData = new []
            {
                new { Name = "Starting Items", Items = stats.Start.Items },
                new { Name = "Early Items", Items = stats.Early.Items },
                new { Name = "Midgame Items", Items = stats.Mid.Items },
                new { Name = "Lategame Items", Items = stats.Late.Items }
            };

            // Create blocks and filter to non-empty blocks
            var blocks = blockData.Select(blockInfo =>
                new ItemSet.Block(blockInfo.Name)
                {
                    items = getItemsWithinMin(blockInfo.Items, min)
                })
                .Where(block => block.items.Count > 0);

            // Add blocks to item set
            itemSet.blocks = new List<ItemSet.Block>(blocks);

            return itemSet;
        }

        private static List<ItemSet.Item> getItemsWithinMin(Dictionary<int, List<ItemPurchaseStats>> items, float min)
        {
            List<ItemSet.Item> itemsInBlock = items.Select(entry => new ItemSet.Item(entry.Key.ToString())
            {
                count = entry.Value.Where(item => item.Percentage >= min).Count()
            }).Where(item => item.count > 0).ToList();

            return itemsInBlock;

            ////Loop through all the items
            //foreach (KeyValuePair<int, List<float>> entry in items)
            //{
            //    //Loop through each item count and percentage
            //    int count = 0;
            //    foreach (float percentage in entry.Value)
            //    {
            //        count++;

            //        //Only add the items that are within the percentage
            //        if (percentage >= min)
            //        {
            //            ItemSet.Item item = new ItemSet.Item(entry.Key.ToString());
            //            item.count = count;
            //            itemList.Add(item);
            //        }
            //    }
            //}
        }
    }

    static class ItemSetWriter
    {
        /// <summary>
        /// Sub path to global item sets
        /// </summary>
        private const string GlobalSubPath = "Config\\Global\\Recommended\\";

        /// <summary>
        /// Sub path to champion item sets (use string.Format to replace {0} with champion key)
        /// </summary>
        private const string ChampionSubPath = "Config\\Champions\\{0}\\Recommended\\";

        /// <summary>
        /// Full file path to league of legends install directory
        /// </summary>
        private static string LeagueOfLegendsPath = "C:\\Riot Games\\League of Legends\\";

        /// <summary>
        /// File extention to use for the file
        /// </summary>
        private const string FileExtention = ".json";

        /// <summary>
        /// Gets the full directory path to where the item sets should go or are stored
        /// </summary>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <returns>Full path to config</returns>
        private static string getItemSetDirectory(string championKey = "")
        {
            if (championKey == "")
                return LeagueOfLegendsPath + GlobalSubPath;
            else
                return LeagueOfLegendsPath +  string.Format(ChampionSubPath, championKey);
        }
        
        /// <summary>
        /// Set the full directory path to the league of legends installation
        /// </summary>
        /// <param name="fullPath">Full directory path to the league of legends installation</param>
        public static void setLeagueOfLegendsPath(string fullPath = "C:\\Riot Games\\League of Legends")
        {
            LeagueOfLegendsPath = fullPath;
        }

        /// <summary>
        /// Get the currently set full directory path to the league of legends installation
        /// </summary>
        /// <returns>Currently set full directory path to the league of legends installation</returns>
        public static string getLeagueOfLegendsPath()
        {
            return LeagueOfLegendsPath;
        }

        /// <summary>
        /// Look through the installed programs registry values and find where league of legends is installed
        /// </summary>
        public static void findLeagueOfLegendsPath()
        {
            foreach(var item in Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall").GetSubKeyNames())
            {
                object programName = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + item).GetValue("DisplayName");
                if(string.Equals(programName, "League of Legends"))
                {
                    object programLocation = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + item).GetValue("InstallLocation");
                    LeagueOfLegendsPath = programLocation.ToString();
                    break;
                }
            }
        }

        /// <summary>
        /// Make sure a directory exists and if not create it
        /// </summary>
        /// <param name="path">directory path to ensure</param>
        private static void ensureDirectory(string path)
        {
            string dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
        }

        /// <summary>
        /// Crate the item set file name based on our naming convention
        /// </summary>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <param name="map">Map type this item is set for</param>
        /// <param name="name">Custom name added to file name</param>
        /// <returns>Name for file including file extention</returns>
        public static string getItemSetFileName(string championKey, ItemSet.Map map, string name)
        {
            //Convert the enum value to the EnumMember string
            var atr = map.GetAttribute<EnumMemberAttribute>();
            string mapAbreviation = atr.Value.ToString();

            return ItemSetNaming.ToolName + championKey + mapAbreviation + name + FileExtention;
        }

        /// <summary>
        /// Write out the item set to a json file in the league of legends item set directory for the given champion key
        /// If directory doesn't exist it will be created so be careful
        /// </summary>
        /// <param name="itemSet">Item set to write out to a json file</param>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <returns>True if item set doesn't exists and was written, false if item set exists already</returns>
        public static bool writeOutItemSet(ItemSet itemSet, string championKey = "", string name = "")
        {
            //Convert our item set to json
            string JSON = JsonConvert.SerializeObject(itemSet, Formatting.Indented);

            //Get the directory we are going to write out to
            string itemSetDir = getItemSetDirectory(championKey);

            //Make sure the directory exists
            ensureDirectory(itemSetDir);

            //Create our unique file name for the system
            string fileName = itemSetDir + getItemSetFileName(championKey, itemSet.map, name);

            //Check if we already have this file
            if (File.Exists(fileName))
                return false;

            //Write out the file
            using (FileStream file = File.Create(fileName))
            {
                using (StreamWriter writer = new StreamWriter(file))
                {
                    writer.Write(JSON);
                }
            }

            return true;
        }

        /// <summary>
        /// Read in an item set from disk
        /// </summary>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <param name="map">Map this item set is for (used in naming convention)</param>
        /// <param name="name">Name for this item set file (used in naming convention)</param>
        /// <returns>Item set if file was found or null if not</returns>
        public static ItemSet readInItemSet(string championKey = "", ItemSet.Map map = ItemSet.Map.SummonersRift, string name = "")
        {
            //Get the directory we are going to write out to
            string itemSetDir = getItemSetDirectory(championKey);

            //Make sure the directory exists
            ensureDirectory(itemSetDir);

            //Create our unique file name for the system
            string fileName = itemSetDir + getItemSetFileName(championKey, map, name);

            //Make sure we have this file
            if (!File.Exists(fileName))
                return null;

            //Read in the file
            using (FileStream file = File.OpenRead(fileName))
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    string JSON = reader.ReadToEnd();
                    ItemSet itemSet = JsonConvert.DeserializeObject<ItemSet>(JSON);
                    return itemSet;
                }
            }
        }
    }
}
