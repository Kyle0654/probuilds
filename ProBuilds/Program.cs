using Newtonsoft.Json;
using RiotSharp;
using RiotSharp.LeagueEndpoint;
using RiotSharp.MatchEndpoint;
using RiotSharp.StaticDataEndpoint;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProBuilds
{
    // TODO: make this a map?
    public class PurchaseTreeNode
    {
        public int ItemId { get; private set; }

        public string ItemName { get; private set; }

        // TODO: maintain a list of edges that lead to this node
        //       we shouldn't probably keep ALL edges, but only summarized edges.
        //       e.g. "HasHighArmor, HasHighAD, HasMediumMR, HasLowAP" etc.
        public ConcurrentDictionary<int, PurchaseTreeNode> NextPurchase = new ConcurrentDictionary<int, PurchaseTreeNode>();

        /// <summary>
        /// How many times this path was taken.
        /// </summary>
        public int Count = 0;

        // TODO: add consumables?

        public PurchaseTreeNode(int itemId)
        {
            ItemId = itemId;
            if (itemId != -1)
                ItemName = StaticDataStore.Items.Items[itemId].Name;
            else
                ItemName = "Root";
        }

        /// <summary>
        /// Merges an item purchase list (pre-filtered of undos and sales) into an existing tree.
        /// </summary>
        /// <param name="itemPurchaseList">The list to merge.</param>
        /// <param name="index">The current position in the list.</param>
        public void Merge(List<ItemPurchaseInformation> itemPurchaseList, int index)
        {
            // Increase count of references to the item
            Interlocked.Increment(ref Count);

            // Get the next item
            ++index;

            if (itemPurchaseList.Count <= index)
                return;

            var item = itemPurchaseList[index];
            int itemId = item.ItemId;

            // Get the next item node
            PurchaseTreeNode nextPurchaseNode = NextPurchase.GetOrAdd(itemId, id => new PurchaseTreeNode(id));

            // Merge the next item
            nextPurchaseNode.Merge(itemPurchaseList, index);
        }

        private IEnumerable<string> ToEnumerable(int depth)
        {
            string prefix = new string('-', depth);
            return Enumerable.Repeat(prefix+ToString(),1).Concat(NextPurchase.Select(kvp => kvp.Value).OrderBy(v => v.Count).SelectMany(v => v.ToEnumerable(depth+1)));
        }

        public string PrintTree()
        {
            return string.Join(Environment.NewLine, ToEnumerable(0));
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}] ({2})", ItemId, StaticDataStore.Items.Items[ItemId].Name, Count);
        }
    }

    public class PurchaseTree : PurchaseTreeNode
    {
        new private int ItemId { get; set; }
        public PurchaseTree() : base(-1) { }
        new private void Merge(List<ItemPurchaseInformation> itemPurchaseList, int index) { }
        public void Merge(List<ItemPurchaseInformation> itemPurchaseList)
        {
            base.Merge(itemPurchaseList, -1);
        }

        public override string ToString()
        {
            return "Root";
        }
        
        public IEnumerable<PurchaseTreeNode> GetBestPath()
        {
            PurchaseTreeNode node = this;
            while (node != null)
            {
                if (node != this) yield return node;
                node = node.NextPurchase.Values.OrderByDescending(n => n.Count).FirstOrDefault();
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Create query parameters
            // TODO: pass these in on command line, or in a settings file?
            RiotQuerySettings querySettings = new RiotQuerySettings(Queue.RankedSolo5x5);

            // Parse api key from args
            if (args.Length == 0)
                return;

            // Check if no-download mode is requested
            // NOTE: will only disable player/match queries
            if (args.Contains("-nodownload"))
            {
                querySettings.NoDownload = true;
                args = args.Where(arg => arg != "-nodownload").ToArray();
            }

            string apiKey = args[0];

            // Check if rates were included in the args
            RiotApi api = null;
            if (args.Length >= 3)
            {
                int rateper10s, rateper10m;
                if (int.TryParse(args[1], out rateper10s) && int.TryParse(args[2], out rateper10m))
                {
                    // Create a production API
                    api = RiotApi.GetInstance(apiKey, rateper10s, rateper10m);
                }
            }

            if (api == null)
            {
                // No rates, create a non-production API
                api = RiotApi.GetInstance(apiKey);
            }

            // Get static API and initialize static data
            StaticRiotApi staticApi = StaticRiotApi.GetInstance(apiKey);
            StaticDataStore.Initialize(staticApi);

            // Create pipeline
            //ChampionWinCounter winCounter = new ChampionWinCounter();
            ItemPurchaseRecorder purchaseRecorder = new ItemPurchaseRecorder();
            MatchPipeline pipeline = new MatchPipeline(api, querySettings, purchaseRecorder);
            pipeline.Process();

            // NOTE: this code is used to write out win rates
            //// Write out champion data
            //var championMatchData = winCounter.ChampionMatchData.ChampionMatchData;
            //championMatchData.Select(kvp =>
            //{
            //    int championId = kvp.Value.ChampionId;
            //    int matchCount = kvp.Value.Matches.Count;
            //    int winCount = kvp.Value.Matches.Count(m => m.Item2);

            //    var champion = StaticDataStore.Champions.Champions.FirstOrDefault(ckvp => ckvp.Value.Id == championId).Value;

            //    return new
            //    {
            //        ChampionId = championId,
            //        MatchCount = matchCount,
            //        WinCount = winCount,
            //        ChampionName = champion.Name
            //    };
            //}).OrderBy(c => c.ChampionName).ToList().ForEach(c =>
            //{
            //    Console.WriteLine("{0} - Matches: {1}, Wins: {2}", c.ChampionName, c.MatchCount, c.WinCount);
            //});

            // Get item purchase data
            var itemPurchases = purchaseRecorder.ItemPurchases.Select(kvp => new
            {
                ChampionId = kvp.Key,
                ChampionName = StaticDataStore.Champions.Champions.FirstOrDefault(ckvp => ckvp.Value.Id == kvp.Key).Value.Name,
                Matches = kvp.Value.Select(m => new
                {
                    MatchId = m.Value.MatchId,
                    Purchases = m.Value.ItemPurchases
                }).ToList()
            }).ToList();


            bool eliminateRecipeComponents = true;

            PurchaseTree purchaseTree = new PurchaseTree();
            var tf = itemPurchases.FirstOrDefault(ip => ip.ChampionName.StartsWith("Zilean"));
            tf.Matches.ForEach(match =>
            {
                // Process out undos
                List<ItemPurchaseInformation> tfpurchases = new List<ItemPurchaseInformation>();
                ItemStatic lastPurchase = null;
                match.Purchases.ForEach(purchase =>
                {
                    switch (purchase.EventType)
                    {
                        case EventType.ItemPurchased:
                        {
                            tfpurchases.Add(purchase);
                            lastPurchase = StaticDataStore.Items.Items[tfpurchases.Last().ItemId];
                            break;
                        }
                        case EventType.ItemUndo:
                        {
                            // Remove last item if we undid a purchase
                            // NOTE: we're not tracking selling back items yet (not a great way to represent that in item sets)
                            // NOTE: we may want to do it later under a heading "starter items - sell these later"
                            // NOTE: we may also want to use the purchase order to represent selling. Like:
                            //       [Guardian Angel] => [Thornmail]
                            //       e.g. "Sell Guardian Angel to purchase Thornmail"
                            if (tfpurchases.Count > 0 && tfpurchases.Last().ItemId == purchase.ItemAfter)
                            {
                                tfpurchases.RemoveAt(tfpurchases.Count - 1);
                                lastPurchase = StaticDataStore.Items.Items[tfpurchases.Last().ItemId];
                            }
                            break;
                        }
                        case EventType.ItemDestroyed:
                        {
                            if (!eliminateRecipeComponents)
                                break;

                            if (lastPurchase == null || lastPurchase.Consumed)
                                break;

                            // List all recipe components of the last purchase.
                            List<int> recipe = new List<int>() { lastPurchase.Id };
                            for (int i = 0; i < recipe.Count; ++i)
                            {
                                var recipeitem = StaticDataStore.Items.Items[recipe[i]];
                                if (recipeitem.From != null)
                                {
                                    var fromids = recipeitem.From.Select(idstring => int.Parse(idstring)).Distinct().Where(id => !recipe.Contains(id));
                                    recipe.AddRange(fromids);
                                }
                            }

                            // If this destroy id matches any components, remove it (the latest) from purchases.
                            if (recipe.Contains(purchase.ItemId))
                            {
                                int lastid = tfpurchases.FindLastIndex(p => p.ItemId == purchase.ItemId);
                                if (lastid != -1)
                                {
                                    tfpurchases.RemoveAt(lastid);
                                }
                            }

                            break;
                        }
                    }
                });

                // Process into tree
                // Filter out consumables
                // NOTE: total biscuit of rejuvenation isn't marked as a consumable.
                var purchaseList = tfpurchases.Where(purchase => !StaticDataStore.Items.Items[purchase.ItemId].Consumed).ToList();
                purchaseTree.Merge(purchaseList);
            });

            // Print purchase tree
            string tree = purchaseTree.PrintTree();
            Console.WriteLine(tree);

            var treejson = JsonConvert.SerializeObject(purchaseTree, Formatting.Indented);

            var bestpath = purchaseTree.GetBestPath().ToList();
            var bestpathstring = string.Join(Environment.NewLine, bestpath.Select(n => n.ToString()));

            // Complete
            Console.WriteLine();
            Console.WriteLine("Complete, press any key to exit");
            Console.ReadKey();
        }
    }
}