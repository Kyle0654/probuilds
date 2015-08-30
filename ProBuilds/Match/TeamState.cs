using RiotSharp.MatchEndpoint;
using System.Collections.Generic;
using System.Linq;

namespace ProBuilds.Match
{
    public class TeamState
    {
        public int TeamId;

        public int TowersDestroyed;
        public Dictionary<TowerType, int> TowerTypesDestroyed = new Dictionary<TowerType, int>();
        public Dictionary<int, ChampionState> Champions;

        public int InhibitorsDestroyed;

        public int DragonKills;
        public int BaronKills;

        public TeamState()
        {
            Champions = new Dictionary<int, ChampionState>();
        }

        public TeamState(int teamId, IEnumerable<int> champions)
        {
            TeamId = teamId;
            Champions = champions.ToDictionary(c => c, c => new ChampionState(c));
        }

        /// <summary>
        /// Clone the team state.
        /// </summary>
        public TeamState Clone()
        {
            TeamState state = new TeamState()
            {
                TeamId = this.TeamId,
                TowersDestroyed = this.TowersDestroyed,
                DragonKills = this.DragonKills,
                BaronKills = this.BaronKills
            };

            foreach (var towerType in TowerTypesDestroyed)
            {
                state.TowerTypesDestroyed.Add(towerType.Key, towerType.Value);
            }
            foreach (var champion in Champions)
            {
                state.Champions.Add(champion.Key, champion.Value.Clone());
            }

            return state;
        }
    }
}