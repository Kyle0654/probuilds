
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
            Start = new PurchaseStats(set.StartPurchases, set.MatchCount);
            Early = new PurchaseStats(set.EarlyPurchases, set.MatchCount);
            Mid = new PurchaseStats(set.MidPurchases, set.MatchCount);
            Late = new PurchaseStats(set.LatePurchases, set.MatchCount);
        }
    }
}