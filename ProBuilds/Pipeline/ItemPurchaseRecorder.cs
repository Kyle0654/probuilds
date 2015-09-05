using ProBuilds.BuildPath;
using ProBuilds.Match;
using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProBuilds.Pipeline
{
    public class ItemPurchaseRecorder : IMatchDetailProcessor
    {
        private int ProcessedCount = 0;

        public int MaxDegreeOfParallelism { get { return 8; } }

        public ConcurrentDictionary<int, ChampionPurchaseTracker> ChampionPurchaseTrackers = new ConcurrentDictionary<int, ChampionPurchaseTracker>();

        private static EventType[] ItemEventTypes = new EventType[] { EventType.ItemPurchased, EventType.ItemDestroyed, EventType.ItemSold, EventType.ItemUndo };
        private static EventType[] SkillEventTypes = new EventType[] { EventType.SkillLevelUp };

        public async Task ConsumeMatchDetail(MatchDetail match)
        {
            int processedId = Interlocked.Increment(ref ProcessedCount);
            Console.WriteLine("Processing Match {0}", processedId);

            Dictionary<int, ChampionMatchItemPurchases> championPurchases = new Dictionary<int, ChampionMatchItemPurchases>();

            var smiteSpell = StaticDataStore.SummonerSpells.SummonerSpells["SummonerSmite"];

            // Get all champions in this match, and initialize recording structures
            foreach (Participant participant in match.Participants)
            {
                int championId = participant.ChampionId;

                //// Create a new match listing and add it to listings
                Lane lane = participant.Timeline.Lane;
                bool hasSmite = (participant.Spell1Id == smiteSpell.Id || participant.Spell2Id == smiteSpell.Id);
                bool isWinner = participant.Stats.Winner;

                ChampionMatchItemPurchases championItemPurchases = new ChampionMatchItemPurchases(championId, match.MatchId, lane, isWinner, hasSmite);

                // For recording, we'll index by participant id so we don't have to look up champion id every time
                championPurchases.Add(participant.ParticipantId, championItemPurchases);
            }

            // Process item purchases
            GameState gameState = new GameState(match);

            // Handle null values
            if (match.Timeline == null || match.Timeline.Frames == null)
                return;

            match.Timeline.Frames.ForEach(frame =>
            {
                if (frame == null ||
                    frame.Events == null)
                    return;

                frame.Events.ForEach(e =>
                {
                    if (e == null)
                        return;

                    // Skip null events
                    if (e.EventType == null)
                        return;

                    // Update any game state that can be gathered from all events
                    gameState.Update(frame, e);

                    // Handle a weird error with ItemId 1501, ItemDestroyed, ParticipantId 0
                    if (!championPurchases.ContainsKey(e.ParticipantId))
                        return;

                    // Process item events
                    if (ItemEventTypes.Contains(e.EventType.Value))
                    {
                        ItemPurchaseInformation itemPurchase = new ItemPurchaseInformation(e, gameState);

                        championPurchases[e.ParticipantId].ItemPurchases.Add(itemPurchase);
                    }

                    // Process skill events
                    if (SkillEventTypes.Contains(e.EventType.Value))
                    {
                        championPurchases[e.ParticipantId].SkillEvents.Add(e);
                    }
                });
            });

            // Analyze match
            championPurchases.Values.AsParallel().WithDegreeOfParallelism(5).ForAll(it =>
            {
                var tracker = ChampionPurchaseTrackers.GetOrAdd(it.ChampionId, id => new ChampionPurchaseTracker(id));
                tracker.Process(it);
            });
        }
    }
}
