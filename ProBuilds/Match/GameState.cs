using Newtonsoft.Json;
using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds.Match
{
    public class GameState
    {
        public TimeSpan Timestamp;

        public Dictionary<int, TeamState> Teams;

        public Dictionary<int, int> ParticipantMap;
        
        #region Helper Properties

        public int TotalKills { get { return Teams.Sum(team => team.Value.Champions.Sum(champion => champion.Value.Kills)); } }
        public int TotalTowerKills { get { return Teams.Sum(team => team.Value.TowersDestroyed); } }
        public int TotalTowerKillsByType(TowerType type)
        {
            return Teams.Sum(team => team.Value.TowerTypesDestroyed.ContainsKey(type) ? team.Value.TowerTypesDestroyed[type] : 0);
        }

        #endregion

        public GameState()
        {
            Teams = new Dictionary<int, TeamState>();
            ParticipantMap = new Dictionary<int, int>();
        }

        public GameState(MatchDetail match)
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
        public GameState Clone()
        {
            GameState state = new GameState()
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
