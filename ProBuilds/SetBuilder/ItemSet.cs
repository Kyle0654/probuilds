using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using ProBuilds.BuildPath;
using ProBuilds.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ProBuilds.SetBuilder
{
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

            [MetadataField]
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

        [MetadataField]
        [JsonIgnore]
        public PurchaseSetKey SetKey;

        [MetadataField]
        [JsonProperty("matchcount")]
        public long MatchCount;

        [MetadataField]
        [JsonProperty("skillorder")]
        public int[] SkillOrder;

        [JsonProperty("title")]
        public string title;

        [MetadataField]
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
}