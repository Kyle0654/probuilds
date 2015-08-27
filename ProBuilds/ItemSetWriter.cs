using Newtonsoft.Json;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.IO;

namespace ProBuilds
{
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

            public Item(string id = "")
            {
                this.id = id;
                this.count = 0;
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
            ItemSet.Item item;
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
        /// Prepend this to all file names written
        /// </summary>
        private const string FileNamePrefix = "PB";

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

            return FileNamePrefix + championKey + mapAbreviation + name + FileExtention;
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
        public static Nullable<ItemSet> readInItemSet(string championKey = "", ItemSet.Map map = ItemSet.Map.SummonersRift, string name = "")
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
