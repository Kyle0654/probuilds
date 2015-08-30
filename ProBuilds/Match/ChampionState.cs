using Newtonsoft.Json;
using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds.Match
{
    public class ChampionState
    {
        public int ChampionId;

        public int Kills;
        public int Deaths;
        public int Assists;

        public List<int> Items;

        /// <summary>
        /// Used to track item combinations.
        /// </summary>
        [JsonIgnore]
        private List<Tuple<int, List<int>>> ItemCombines;

        public ChampionState() { }

        public ChampionState(int championId)
        {
            ChampionId = championId;
            Items = new List<int>();
            ItemCombines = new List<Tuple<int, List<int>>>();
        }

        public ChampionState Clone()
        {
            ChampionState state = new ChampionState()
            {
                ChampionId = this.ChampionId,
                Kills = this.Kills,
                Deaths = this.Deaths,
                Assists = this.Assists,
                Items = new List<int>(this.Items)
            };

            return state;
        }

        #region Item Handling

        internal void ItemPurchased(int itemId)
        {
            Items.Add(itemId);
            ItemCombines.Add(new Tuple<int, List<int>>(itemId, new List<int>()));
        }

        internal void ItemSold(int itemId)
        {
            Items.Remove(itemId);
        }

        internal void ItemDestroyed(int itemId)
        {
            Items.Remove(itemId);

            // Keep track of combined items for undo
            if (!StaticDataStore.Items.Items[itemId].Consumed && ItemCombines.Count > 0)
            {
                ItemCombines[ItemCombines.Count - 1].Item2.Add(itemId);
            }
        }

        internal void ItemUndo(int itemBefore, int itemAfter)
        {
            if (itemBefore != 0)
            {
                Items.Remove(itemBefore);

                // Restore any combined items that were destroyed as part of the purchase being undone.
                var purchase = ItemCombines.LastOrDefault();
                if (purchase != null)
                {
                    ItemCombines.RemoveAt(ItemCombines.Count - 1);
                    Items.AddRange(purchase.Item2);
                }
            }

            if (itemAfter != 0)
                Items.Add(itemAfter);
        }

        #endregion
    }
}