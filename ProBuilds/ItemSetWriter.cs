using Newtonsoft.Json;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace ProBuilds
{
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
            this.type = Type.Custom;
            this.map = Map.any;
            this.mode = Mode.any;
            this.priority = false;
            this.sortrank = 0;
            this.blocks = null;
        }
    }

    class ItemSetWriter
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
        /// Gets the full directory path to where the item sets should go or are stored
        /// </summary>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <returns>Full path to confi</returns>
        private string getItemSetDirectory(string championKey = "")
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
    }
}
