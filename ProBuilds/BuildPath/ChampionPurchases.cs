using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds.BuildPath
{
    public class ChampionPurchases
    {
        public int ChampionId { get; private set; }
        public string ChampionName { get; private set;}
        public Dictionary<long, List<ItemPurchaseInformation>> Matches { get; private set; }

        /// <summary>
        /// Convert an item purchase recorder to a dictionary of champion item purchases.
        /// </summary>
        public static Dictionary<int, ChampionPurchases> Create(ItemPurchaseRecorder recorder)
        {
            //var purchases = recorder.ItemPurchases.Select(kvp =>
            //    new ChampionPurchases()
            //    {
            //        ChampionId = kvp.Key,
            //        ChampionName = StaticDataStore.Champions.Champions.FirstOrDefault(ckvp => ckvp.Value.Id == kvp.Key).Value.Name,
            //        Matches = kvp.Value.ToDictionary(m => m.Value.MatchId, m => m.Value.ItemPurchases)
            //    }
            //).ToDictionary(p => p.ChampionId, p => p);

            //return purchases;
            return null;
        }
    }
}
