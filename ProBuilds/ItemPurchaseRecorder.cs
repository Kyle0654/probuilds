using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public class GameState
    {
        // TODO: fill this out over the game
        public TimeSpan Timestamp;

        public GameState() { }

        /// <summary>
        /// Create a clone of the game state (always use this when storing a copy, as it will change during processing).
        /// </summary>
        /// <returns></returns>
        public GameState Clone()
        {
            return new GameState()
            {
                Timestamp = this.Timestamp
            };
        }
    }

    public class ItemPurchaseInformation
    {
        public int ItemId;
        public int ItemBefore;
        public int ItemAfter;

        public EventType EventType;

        public GameState GameState;

        public ItemPurchaseInformation() { }

        public ItemPurchaseInformation(Event itemEvent, GameState gameState)
        {
            ItemId = itemEvent.ItemId;
            ItemBefore = itemEvent.ItemBefore;
            ItemAfter = itemEvent.ItemAfter;
            EventType = itemEvent.EventType ?? EventType.AscendedEvent; // AscendedEvent is a failure case

            if (EventType == RiotSharp.MatchEndpoint.EventType.AscendedEvent)
            {
                throw new ArgumentException("Event type must not be null.", "itemEvent");
            }

            GameState = gameState.Clone();
        }

        public override string ToString()
        {
            if (EventType == RiotSharp.MatchEndpoint.EventType.ItemUndo)
            {
                string itemBeforeString = ItemBefore == 0 ? "0" : string.Format("{0} [{1}]", ItemBefore, StaticDataStore.Items.Items[ItemBefore].Name);
                string itemAfterString = ItemAfter == 0 ? "0" : string.Format("{0} [{1}]", ItemAfter, StaticDataStore.Items.Items[ItemAfter].Name);
                return string.Format("{0}: {1} => {2}", EventType.ToString(), itemBeforeString, itemAfterString);
            }
            else
            {
                return string.Format("{0}: {1} [{2}]", EventType.ToString(), ItemId, StaticDataStore.Items.Items[ItemId].Name);
            }
        }
    }

    public class ChampionMatchItemPurchases
    {
        public int ChampionId { get; private set; }
        public long MatchId { get; private set; }
        public List<ItemPurchaseInformation> ItemPurchases { get; private set; }

        public ChampionMatchItemPurchases(int championId, long matchId)
        {
            ChampionId = championId;
            MatchId = matchId;
            ItemPurchases = new List<ItemPurchaseInformation>();
        }
    }

    public class ItemPurchaseRecorder : IMatchDetailProcessor
    {
        public int MaxDegreeOfParallelism { get { return 8; } }
        public int BoundedCapacity { get { return 128; } }

        public ConcurrentDictionary<int, ConcurrentDictionary<long, ChampionMatchItemPurchases>> ItemPurchases = new ConcurrentDictionary<int, ConcurrentDictionary<long, ChampionMatchItemPurchases>>();

        private static EventType[] ItemEventTypes = new EventType[] { EventType.ItemPurchased, EventType.ItemDestroyed, EventType.ItemSold, EventType.ItemUndo };

        public async Task ConsumeMatchDetail(MatchDetail match)
        {
            Dictionary<int, ChampionMatchItemPurchases> championPurchases = new Dictionary<int, ChampionMatchItemPurchases>();

            // Get all champions in this match, and initialize recording structures
            foreach (Participant participant in match.Participants)
            {
                int championId = participant.ChampionId;

                // Get the match listing for this champion (create if it doesn't exist)
                ConcurrentDictionary<long, ChampionMatchItemPurchases> championMatches =
                    ItemPurchases.GetOrAdd(championId, id => new ConcurrentDictionary<long, ChampionMatchItemPurchases>());

                // Create a new match listing and add it to listings
                ChampionMatchItemPurchases championItemPurchases = new ChampionMatchItemPurchases(championId, match.MatchId);
                if (!championMatches.TryAdd(match.MatchId, championItemPurchases))
                {
                    // We have already tried to process this match for some reason...
                    throw new ArgumentException("Match has already been processed.", "match");
                }

                // For recording, we'll index by participant id so we don't have to look up champion id every time
                championPurchases.Add(participant.ParticipantId, championItemPurchases);
            }

            // Process item purchases
            GameState gameState = new GameState();

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
                    gameState.Timestamp = e.Timestamp;

                    // Process item events
                    if (ItemEventTypes.Contains(e.EventType.Value))
                    {
                        ItemPurchaseInformation itemPurchase = new ItemPurchaseInformation(e, gameState);

                        // Handle a weird error with ItemId 1501, ItemDestroyed, ParticipantId 0
                        if (!championPurchases.ContainsKey(e.ParticipantId))
                            return;

                        championPurchases[e.ParticipantId].ItemPurchases.Add(itemPurchase);
                    }
                });
            });
        }
    }
}
