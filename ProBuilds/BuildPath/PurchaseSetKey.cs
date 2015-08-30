using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProBuilds.Match;
using RiotSharp.MatchEndpoint;
using System;

namespace ProBuilds.BuildPath
{
    /// <summary>
    /// Key for a purchase set.
    /// </summary>
    /// <remarks>This key contains data that should significantly differentiate item purchases throughout a match.</remarks>
    public class PurchaseSetKey
    {
        public int ChampionId { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Lane Lane { get; private set; }
        public bool HasSmite { get; private set; }

        public PurchaseSetKey(ChampionMatchItemPurchases matchPurchases)
        {
            ChampionId = matchPurchases.ChampionId;
            Lane = matchPurchases.Lane;
            HasSmite = matchPurchases.HasSmite;

            // Convert all "Bot" to "Bottom"
            if (Lane == RiotSharp.MatchEndpoint.Lane.Bot)
                Lane = RiotSharp.MatchEndpoint.Lane.Bottom;
        }

        #region Equality

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (obj == null)
                return true;

            if (!(obj is PurchaseSetKey))
                return false;

            PurchaseSetKey other = obj as PurchaseSetKey;

            return
            ChampionId == other.ChampionId &&
            Lane == other.Lane &&
            HasSmite == other.HasSmite;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(ChampionId, Lane, HasSmite).GetHashCode();
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0} {1} {2}",
            StaticDataStore.Champions != null ?
            StaticDataStore.Champions.Champions[StaticDataStore.Champions.Keys[ChampionId]].Name :
            ChampionId.ToString(),
            Lane.ToString(),
            HasSmite ? "smite" : "no-smite");
        }
    }
}