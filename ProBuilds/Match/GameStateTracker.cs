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

        public ChampionState() { }

        public ChampionState(int championId)
        {
            ChampionId = championId;
            Items = new List<int>();
        }

        public ChampionState Clone()
        {
            ChampionState state = new ChampionState(ChampionId)
            {
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
        }

        internal void ItemSold(int itemId)
        {
            Items.Remove(itemId);
        }

        internal void ItemDestroyed(int itemId)
        {
            Items.Remove(itemId);
        }

        internal void ItemUndo(int itemBefore, int itemAfter)
        {
            if (itemBefore != 0)
                Items.Remove(itemBefore);

            if (itemAfter != 0)
                Items.Add(itemAfter);
        }

        #endregion
    }

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
                state.Champions.Add(champion.Key, champion.Value);
            }

            return state;
        }
    }

    public class GameStateTracker
    {
        // TODO: fill this out over the game
        public TimeSpan Timestamp;

        public Dictionary<int, TeamState> Teams;

        public Dictionary<int, int> ParticipantMap;

        public GameStateTracker()
        {
            Teams = new Dictionary<int, TeamState>();
            ParticipantMap = new Dictionary<int, int>();
        }

        public GameStateTracker(MatchDetail match)
        {
            // TODO: track champion lanes/roles/etc.

            Teams = match.Teams.ToDictionary(
                t => t.TeamId,
                t => new TeamState(t.TeamId,
                    match.Participants.Where(p => p.TeamId == t.TeamId).Select(p => p.ChampionId)));

            ParticipantMap = match.Participants.ToDictionary(
                p => p.ParticipantId,
                p => p.ChampionId);
        }

        /// <summary>
        /// Create a clone of the game state (always use this when storing a copy, as it will change during processing).
        /// </summary>
        /// <returns></returns>
        public GameStateTracker Clone()
        {
            GameStateTracker state = new GameStateTracker()
            {
                Timestamp = this.Timestamp
            };

            foreach (var team in Teams)
            {
                state.Teams.Add(team.Key, team.Value.Clone());
            }
            foreach (var participant in ParticipantMap)
            {
                state.ParticipantMap.Add(participant.Key, participant.Value);
            }

            return state;
        }

        /// <summary>
        /// Updates the game state with a timeline event.
        /// </summary>
        /// <remarks>Events are expected to happen in the order this is called.</remarks>
        public void Update(Frame frame, Event e)
        {
            if (!e.EventType.HasValue)
                return;

            // Call event handlers.
            // NOTE: We only handle events that could be used to make a decision about item purchases.
            switch (e.EventType.Value)
            {
                case EventType.BuildingKill:
                    HandleBuildingKill(frame, e);
                    break;
                case EventType.ChampionKill:
                    HandleChampionKill(frame, e);
                    break;
                case EventType.EliteMonsterKill:
                    HandleEliteMonsterKill(frame, e);
                    break;
                case EventType.ItemDestroyed:
                    HandleItemDestroyed(frame, e);
                    break;
                case EventType.ItemPurchased:
                    HandleItemPurchased(frame, e);
                    break;
                case EventType.ItemSold:
                    HandleItemSold(frame, e);
                    break;
                case EventType.ItemUndo:
                    HandleItemUndo(frame, e);
                    break;
                default:
                    return;
            }

            // If the event was handled, update the timestamp
            Timestamp = e.Timestamp;
        }

        #region Helpers

        public ChampionState GetChampion(int championId)
        {
            var team = Teams.Values.FirstOrDefault(t => t.Champions.ContainsKey(championId));
            return team.Champions[championId];
        }

        public TeamState GetTeamByParticipant(int participantId)
        {
            int championId = ParticipantMap[participantId];
            var team = Teams.Values.FirstOrDefault(t => t.Champions.ContainsKey(championId));
            return team;
        }

        public ChampionState GetChampionByParticipant(int participantId)
        {
            return GetChampion(ParticipantMap[participantId]);
        }

        #endregion

        #region Event Handlers

        private void HandleBuildingKill(Frame frame, Event e)
        {
            if (!e.BuildingType.HasValue)
                return;

            // Team that destroyed a tower
            int teamId = e.TeamId;

            switch (e.BuildingType.Value)
            {
                case BuildingType.InhibitorBuilding:
                    ++Teams[teamId].InhibitorsDestroyed;
                    break;
                case BuildingType.TowerBuilding:
                    {
                        ++Teams[teamId].TowersDestroyed;

                        if (!e.TowerType.HasValue)
                            return;

                        if (Teams[teamId].TowerTypesDestroyed.ContainsKey(e.TowerType.Value))
                        {
                            ++Teams[teamId].TowerTypesDestroyed[e.TowerType.Value];
                        }
                        else
                        {
                            Teams[teamId].TowerTypesDestroyed.Add(e.TowerType.Value, 1);
                        }
                    }
                    break;
            }
        }

        private void HandleChampionKill(Frame frame, Event e)
		{
            int killerId = e.KillerId;
            int victimId = e.VictimId;
            List<int> assistIds = e.AssistingParticipantIds;

            if (killerId != 0) // 0 = minion
            {
                ++GetChampionByParticipant(killerId).Kills;
            }

            ++GetChampionByParticipant(victimId).Deaths;

            if (assistIds != null)
            {
                assistIds.ForEach(assistId => ++GetChampionByParticipant(assistId).Assists);
            }
		}

        private void HandleEliteMonsterKill(Frame frame, Event e)
		{
            if (!e.MonsterType.HasValue)
                return;

            var team = (e.KillerId != 0) ? GetTeamByParticipant(e.KillerId) : (e.TeamId != 0) ? Teams[e.TeamId] : null;

            // This happens sometimes - not sure why
            if (team == null)
                return;

            switch (e.MonsterType.Value)
            {
                case MonsterType.BaronNashor:
                    ++team.BaronKills;
                    break;
                case MonsterType.Dragon:
                    ++team.DragonKills;
                    break;
            }
		}

        private void HandleItemDestroyed(Frame frame, Event e)
		{
            // Handle a bug where we get an ItemDestroyed with no participant
            if (e.ParticipantId == 0)
                return;

            GetChampionByParticipant(e.ParticipantId).ItemDestroyed(e.ItemId);
		}

        private void HandleItemPurchased(Frame frame, Event e)
		{
            GetChampionByParticipant(e.ParticipantId).ItemPurchased(e.ItemId);
		}

        private void HandleItemSold(Frame frame, Event e)
		{
            GetChampionByParticipant(e.ParticipantId).ItemSold(e.ItemId);
		}

        private void HandleItemUndo(Frame frame, Event e)
		{
            GetChampionByParticipant(e.ParticipantId).ItemUndo(e.ItemBefore, e.ItemAfter);
		}

        #endregion
    }
}
