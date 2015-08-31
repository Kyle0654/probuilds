using ProBuilds.Match;
using RiotSharp.MatchEndpoint;
using System;
using System.Linq;
using System.Collections.Generic;
using RiotSharp.StaticDataEndpoint;

namespace ProBuilds.Match
{
    /// <summary>
    /// Purchase information, adding game state information to a purchase event.
    /// </summary>
    public class ItemPurchaseInformation
    {
        public int ItemId;
        public int ItemBefore;
        public int ItemAfter;

        public EventType EventType;

        /// <summary>
        /// The state of the game when this purchase was made.
        /// </summary>
        public GameState GameState;

        /// <summary>
        /// The item in this event (or null if the item doesn't exist, or this is an undo).
        /// </summary>
        public ItemStatic Item
        {
            get { return StaticDataStore.Items.Items.ContainsKey(ItemId) ? StaticDataStore.Items.Items[ItemId] : null; }
        }

        /// <summary>
        /// Original purchase events for items that built into this item.
        /// </summary>
        public List<ItemPurchaseInformation> BuiltFrom = new List<ItemPurchaseInformation>();

        /// <summary>
        /// Items that built into this item.
        /// </summary>
        public IEnumerable<int> BuiltFromItems { get { return BuiltFrom.Select(purchase => purchase.ItemId); } }

        /// <summary>
        /// Purchase that this purchase eventually builds into.
        /// </summary>
        public ItemPurchaseInformation BuildsInto = null;

        /// <summary>
        /// Returns the item that this purchase eventually builds into.
        /// </summary>
        /// <returns>The item id that this purchase builds into, or -1 if none.</returns>
        public int BuildsIntoItemId { get { return BuildsInto == null ? -1 : BuildsInto.ItemId; } }

        /// <summary>
        /// Returns the final purchase that this item eventually combined into.
        /// </summary>
        public ItemPurchaseInformation FinalBuildItem { get { return BuildsInto == null ? this : BuildsInto.FinalBuildItem; } }

        /// <summary>
        /// Whether or not this item eventually builds into another within this match.
        /// </summary>
        public bool IsRecipeComponent { get { return BuildsInto != null; } }

        /// <summary>
        /// The event that sells this item.
        /// </summary>
        public ItemPurchaseInformation SoldBy = null;

        /// <summary>
        /// The item that this event sells.
        /// </summary>
        public ItemPurchaseInformation Sells = null;

        /// <summary>
        /// Whether or not this item is eventually sold.
        /// </summary>
        public bool IsSold { get { return SoldBy != null; } }

        /// <summary>
        /// The event that destroys this item.
        /// </summary>
        public ItemPurchaseInformation DestroyedBy = null;

        /// <summary>
        /// The item that this event destroys.
        /// </summary>
        public ItemPurchaseInformation Destroys = null;

        /// <summary>
        /// Whether or not this item is eventually sold.
        /// </summary>
        public bool IsDestroyed { get { return DestroyedBy != null; } }

        /// <summary>
        /// Whether or not this item is in the champion's inventory.
        /// </summary>
        public bool IsInInventory { get { return !IsSold && !IsDestroyed; } }

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