using ProBuilds.Match;
using RiotSharp.MatchEndpoint;
using System;

namespace ProBuilds.Match
{
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

            if (!itemEvent.EventType.HasValue)
            {
                throw new ArgumentException("Event type must not be null.", "itemEvent");
            }

            EventType = itemEvent.EventType.Value;

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
}