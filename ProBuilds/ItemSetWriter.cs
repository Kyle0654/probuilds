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
using Newtonsoft.Json.Serialization;

namespace ProBuilds
{
    /// <summary>
    /// This is used in item set naming conventions
    /// </summary>
    static class ItemSetNaming
    {
        public const string ToolName = "PBIS";
    }

    /// <summary>
    /// Structure that holds everything in an item set to write out to JSON
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public struct ItemSet
    {
        [JsonObject(MemberSerialization.OptIn)]
        public struct Item
        {
            [JsonProperty("id")]
            public string id;

            [JsonProperty("count")]
            public int count;

            [JsonExtraField]
            [JsonProperty("percentage")]
            public float percentage;

            public Item(string id = "")
            {
                this.id = id;
                this.count = 0;
                this.percentage = 0.0f;
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public struct Block
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

        [JsonProperty("title")]
        public string title;

        [JsonExtraField]
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
            ItemSet set;
            set.title = championKey + " ProBuilds";
            set.description = "this is a test.";
            set.type = ItemSet.Type.Custom;
            set.map = ItemSet.Map.SummonersRift;
            set.mode = ItemSet.Mode.any;
            set.priority = false;
            set.sortrank = 0;
            set.blocks = new List<ItemSet.Block>();
            ItemSet.Block block;
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
            Nullable<ItemSet> set = ItemSetWriter.readInItemSet(championKey, map, "Boots");

            if(set != null)
            {
                string JSON = JsonConvert.SerializeObject(set.Value, Formatting.Indented);
                Console.Write(JSON);
            }
            else
            {
                Console.WriteLine("No item set found");
            }
        }

        public static void testGenerator(Dictionary<int, ChampionPurchaseStats> championPurchaseStats)
        {
            //Dictionary<int, ItemSet> itemSets = new Dictionary<int, ItemSet>();
            //ItemSetGenerator.generateAll(championPurchaseStats, 0.5f, out itemSets);
            ChampionStatic champInfo;
            if (StaticDataStore.Champions.Champions.TryGetValue("Ashe", out champInfo))
            {
                ChampionPurchaseStats champStats;
                if (championPurchaseStats.TryGetValue(champInfo.Id, out champStats))
                {
                    ItemSet itemSet;
                    ItemSetGenerator.generate(champStats.ChampionId, champStats, 0.5f, out itemSet);
                    ItemSetWriter.writeOutItemSet(itemSet, champInfo.Key);
                }
            }
        }
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
        public static bool generateAll(Dictionary<int, ChampionPurchaseStats> championPurchaseStats, float min, out Dictionary<int, ItemSet> itemSets)
        {
            //Create a dictionary we can use in threading
            System.Collections.Concurrent.ConcurrentDictionary<int, ItemSet> threadedSets = new System.Collections.Concurrent.ConcurrentDictionary<int, ItemSet>();

            //Loop through all our champions we have stats for and generate sets for them
            Parallel.ForEach(championPurchaseStats, entry =>
            {
                //Generate a set for this champion
                ItemSet itemSet = new ItemSet();

                if (generate(entry.Key, entry.Value, min, out itemSet))
                {
                    threadedSets.TryAdd(entry.Key, itemSet);
                }
            });

            itemSets = new Dictionary<int, ItemSet>(threadedSets);

            return (itemSets.Count > 0);
        }

        public static bool generate(int championId, ChampionPurchaseStats stats, float min, out ItemSet itemSet)
        {
            //Lookup the champion
            ChampionStatic championInfo = StaticDataStore.Champions.Champions.First().Value;
            string name = "";
            bool champIssue = false;
            if (StaticDataStore.Champions.Keys.TryGetValue(championId, out name))
            {
                if (!StaticDataStore.Champions.Champions.TryGetValue(name, out championInfo))
                {
                    champIssue = true;
                }
            }
            else
            {
                champIssue = true;
            }

            if(champIssue)
            {
                itemSet = new ItemSet();
                return false;
            }

            //Come up with an item set title
            itemSet.title = championInfo.Key + " " + (min * 100).ToString() + " " + ItemSetNaming.ToolName;

            //Add a nice description of how this was generated
            itemSet.description = "Item set generated using " + stats.MatchCount.ToString() + " matches and only taking items used at least " + (min * 100).ToString() + "% of the time.";

            //Fill out a bunch of other stuff that is the same for this tool
            itemSet.type = type;
            itemSet.map = map;
            itemSet.mode = mode;
            itemSet.priority = priority;
            itemSet.sortrank = sortRank < 0 ? (int)Math.Round(min * 100) : sortRank;

            //Create the blocks
            itemSet.blocks = new List<ItemSet.Block>(4);

            //Create the starting items block
            ItemSet.Block blockStart = new ItemSet.Block("Starting Items");
            blockStart.items = new List<ItemSet.Item>();
            addItemsWithinMin(stats.Start.Items, ref blockStart.items, min);
            if (blockStart.items.Count > 0)
            {
                itemSet.blocks.Add(blockStart);
            }

            //Create the Earlygame items block
            ItemSet.Block blockEarly = new ItemSet.Block("Early Items");
            blockEarly.items = new List<ItemSet.Item>();
            addItemsWithinMin(stats.Early.Items, ref blockEarly.items, min);
            if (blockEarly.items.Count > 0)
            {
                itemSet.blocks.Add(blockEarly);
            }

            //Create the Midgame items block
            ItemSet.Block blockMid = new ItemSet.Block("Midgame Items");
            blockMid.items = new List<ItemSet.Item>();
            addItemsWithinMin(stats.Mid.Items, ref blockMid.items, min);
            if (blockMid.items.Count > 0)
            {
                itemSet.blocks.Add(blockMid);
            }

            //Create the Lategame items block
            ItemSet.Block blockLate = new ItemSet.Block("Lategame Items");
            blockLate.items = new List<ItemSet.Item>();
            addItemsWithinMin(stats.Late.Items, ref blockLate.items, min);
            if (blockLate.items.Count > 0)
            {
                itemSet.blocks.Add(blockLate);
            }

            return true;
        }

        private static void addItemsWithinMin(Dictionary<int, List<float>> items, ref List<ItemSet.Item> itemList, float min)
        {
            List<ItemSet.Item> itemsInBlock = items.Select(entry => new ItemSet.Item(entry.Key.ToString())
            {
                count = entry.Value.Where(percentage => percentage >= min).Count(),
                percentage = entry.Value.LastOrDefault(percentage => percentage >= min)
            }).Where(item => item.count > 0).ToList();

            itemList.AddRange(itemsInBlock);

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
                return Path.Combine(LeagueOfLegendsPath, GlobalSubPath);
            else
                return Path.Combine(LeagueOfLegendsPath, string.Format(ChampionSubPath, championKey));
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
        /// <param name="name">Unique name to append to this ItemSet</param>
        /// <param name="writeExtraFields">True to write out any fields taged as extra, false to not</param>
        /// <param name="subDir">Sub directory to write to, if empty use League of Legends directory</param>
        /// <returns>True if item set doesn't exists and was written, false if item set exists already</returns>
        public static bool writeOutItemSet(ItemSet itemSet, string championKey = "", string name = "", bool writeExtraFields = true, string subDir = "")
        {
            //Setup our custom serialization settings
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            if (!writeExtraFields)
                settings.ContractResolver = new ExtraFieldContractResolver();

            //Convert our item set to json
            string JSON = JsonConvert.SerializeObject(itemSet, settings);

            //Get the directory we are going to write out to
            string itemSetDir = (String.IsNullOrEmpty(subDir)) ? getItemSetDirectory(championKey) : Path.Combine(subDir, championKey == "" ? "Global" : championKey) + Path.DirectorySeparatorChar;

            //Make sure the directory exists
            ensureDirectory(itemSetDir);

            //Create our unique file name for the system
            string fileName = Path.Combine(itemSetDir, getItemSetFileName(championKey, itemSet.map, name));

            //Check if we already have this file
            if (File.Exists(fileName))
                return false;

            //Write out the file
            File.WriteAllText(fileName, JSON);

            return true;
        }

        /// <summary>
        /// Read in an item set from disk
        /// </summary>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <param name="map">Map this item set is for (used in naming convention)</param>
        /// <param name="name">Name for this item set file (used in naming convention)</param>
        /// <param name="subDir">Sub directory to read from, if empty use League of Legends directory</param>
        /// <returns>Item set if file was found or null if not</returns>
        public static Nullable<ItemSet> readInItemSet(string championKey = "", ItemSet.Map map = ItemSet.Map.SummonersRift, string name = "", string subDir = "")
        {
            //Get the directory we are going to write out to
            string itemSetDir = (String.IsNullOrEmpty(subDir)) ? getItemSetDirectory(championKey) : Path.Combine(subDir, championKey == "" ? "Global" : championKey) + Path.DirectorySeparatorChar;

            //Create our unique file name for the system
            string fileName = Path.Combine(itemSetDir, getItemSetFileName(championKey, map, name));

            //Make sure we have this file
            if (!File.Exists(fileName))
                return null;

            //Read in the file
            string JSON = File.ReadAllText(fileName);

            //Convert the JSON string to an object
            ItemSet itemSet = JsonConvert.DeserializeObject<ItemSet>(JSON);
            return itemSet;
        }
    }
}
