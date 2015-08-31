using ProBuilds.SetBuilder;
using System.Linq;

namespace ProBuilds.BuildPath
{
    /// <summary>
    /// Stats for all states of the game.
    /// </summary>
    public class ChampionPurchaseStats
    {
        public PurchaseSetKey Key;

        public int ChampionId { get { return Key.ChampionId; } }
        public long MatchCount;

        public PurchaseStats Start;
        public PurchaseStats Early;
        public PurchaseStats Mid;
        public PurchaseStats Late;

        public ChampionPurchaseStats(PurchaseSet set)
        {
            Key = set.Key;

            MatchCount = set.MatchCount;

            // Split purchases to game stage
            var startItems = set.AllItemPurchases.Where(SetBuilderSettings.IsStartPurchase);
            var earlyItems = set.AllItemPurchases.Where(SetBuilderSettings.IsEarlyPurchase);
            var midItems = set.AllItemPurchases.Where(SetBuilderSettings.IsMidPurchase);
            var lateItems = set.AllItemPurchases.Where(SetBuilderSettings.IsLatePurchase);

            // Create stats
            Start = new PurchaseStats(startItems, set.MatchCount);
            Early = new PurchaseStats(earlyItems, set.MatchCount);
            Mid = new PurchaseStats(midItems, set.MatchCount);
            Late = new PurchaseStats(lateItems, set.MatchCount);
        }
    }
}