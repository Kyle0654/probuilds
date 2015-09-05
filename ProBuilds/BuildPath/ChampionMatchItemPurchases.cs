using ProBuilds.Match;
using RiotSharp.MatchEndpoint;
using System.Collections.Generic;
using System.Linq;

namespace ProBuilds.BuildPath
{
    public class ChampionMatchItemPurchases
    {
        public int ChampionId { get; private set; }
        public long MatchId { get; private set; }

        public Lane Lane { get; private set; }
        public bool IsWinner { get; private set; }
        public bool HasSmite { get; private set; }

        public List<ItemPurchaseInformation> ItemPurchases { get; private set; }
        public List<Event> SkillEvents { get; private set; }

        public IEnumerable<int> SkillOrder { get { return SkillEvents.Select(e => e.SkillSlot); } }

        public ChampionMatchItemPurchases(int championId, long matchId, Lane lane, bool isWinner, bool hasSmite)
        {
            ChampionId = championId;
            MatchId = matchId;

            Lane = lane;
            IsWinner = isWinner;
            HasSmite = hasSmite;

            ItemPurchases = new List<ItemPurchaseInformation>();
            SkillEvents = new List<Event>();
        }
    }
}