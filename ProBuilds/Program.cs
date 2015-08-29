using Newtonsoft.Json;
using ProBuilds.BuildPath;
using ProBuilds.SetBuilder;
using RiotSharp;
using RiotSharp.LeagueEndpoint;
using RiotSharp.MatchEndpoint;
using RiotSharp.StaticDataEndpoint;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

            // Get stats
            var championPurchaseStats =
                purchaseRecorder.ChampionPurchaseTrackers.ToDictionary(
                    kvp => kvp.Value.ChampionId,
                    kvp => kvp.Value.GetStats());

            #region Test Code

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

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            //// Get item purchase data
            //var itemPurchases = purchaseRecorder.ItemPurchases.Select(kvp => new
            //{
            //    ChampionId = kvp.Key,
            //    ChampionName = StaticDataStore.Champions.Champions.FirstOrDefault(ckvp => ckvp.Value.Id == kvp.Key).Value.Name,
            //    Matches = kvp.Value.Select(m => new
            //    {
            //        MatchId = m.Value.MatchId,
            //        Purchases = m.Value.ItemPurchases
            //    }).ToList()
            //}).ToList();


            //bool eliminateRecipeComponents = true;

            //PurchaseTree purchaseTree = new PurchaseTree();
            //var tf = itemPurchases.FirstOrDefault(ip => ip.ChampionName.StartsWith("Zilean"));
            //tf.Matches.ForEach(match =>
            //{
            //    // Process out undos
            //    List<ItemPurchaseInformation> tfpurchases = new List<ItemPurchaseInformation>();
            //    ItemStatic lastPurchase = null;
            //    match.Purchases.ForEach(purchase =>
            //    {
            //        switch (purchase.EventType)
            //        {
            //            case EventType.ItemPurchased:
            //            {
            //                tfpurchases.Add(purchase);
            //                lastPurchase = StaticDataStore.Items.Items[tfpurchases.Last().ItemId];
            //                break;
            //            }
            //            case EventType.ItemUndo:
            //            {
            //                // Remove last item if we undid a purchase
            //                // NOTE: we're not tracking selling back items yet (not a great way to represent that in item sets)
            //                // NOTE: we may want to do it later under a heading "starter items - sell these later"
            //                // NOTE: we may also want to use the purchase order to represent selling. Like:
            //                //       [Guardian Angel] => [Thornmail]
            //                //       e.g. "Sell Guardian Angel to purchase Thornmail"
            //                if (tfpurchases.Count > 0 && tfpurchases.Last().ItemId == purchase.ItemAfter)
            //                {
            //                    tfpurchases.RemoveAt(tfpurchases.Count - 1);
            //                    lastPurchase = StaticDataStore.Items.Items[tfpurchases.Last().ItemId];
            //                }
            //                break;
            //            }
            //            case EventType.ItemDestroyed:
            //            {
            //                if (!eliminateRecipeComponents)
            //                    break;

            //                if (lastPurchase == null || lastPurchase.Consumed)
            //                    break;

            //                // List all recipe components of the last purchase.
            //                List<int> recipe = new List<int>() { lastPurchase.Id };
            //                for (int i = 0; i < recipe.Count; ++i)
            //                {
            //                    var recipeitem = StaticDataStore.Items.Items[recipe[i]];
            //                    if (recipeitem.From != null)
            //                    {
            //                        var fromids = recipeitem.From.Select(idstring => int.Parse(idstring)).Distinct().Where(id => !recipe.Contains(id));
            //                        recipe.AddRange(fromids);
            //                    }
            //                }

            //                // If this destroy id matches any components, remove it (the latest) from purchases.
            //                if (recipe.Contains(purchase.ItemId))
            //                {
            //                    int lastid = tfpurchases.FindLastIndex(p => p.ItemId == purchase.ItemId);
            //                    if (lastid != -1)
            //                    {
            //                        tfpurchases.RemoveAt(lastid);
            //                    }
            //                }

            //                break;
            //            }
            //        }
            //    });

            //    // Process into tree
            //    // Filter out consumables
            //    // NOTE: total biscuit of rejuvenation isn't marked as a consumable.
            //    var purchaseList = tfpurchases.Where(purchase => !StaticDataStore.Items.Items[purchase.ItemId].Consumed).ToList();
            //    purchaseTree.Merge(purchaseList);
            //});

            //// Print purchase tree
            //string tree = purchaseTree.PrintTree();
            //Console.WriteLine(tree);

            //var treejson = JsonConvert.SerializeObject(purchaseTree, Formatting.Indented);

            //var bestpath = purchaseTree.GetBestPath().ToList();
            //var bestpathstring = string.Join(Environment.NewLine, bestpath.Select(n => n.ToString()));

            #endregion

            // Generate item sets
            Dictionary<PurchaseSetKey, ItemSet> itemSets = ItemSetGenerator.generateAll(championPurchaseStats, SetBuilderSettings.ItemMinimumPurchasePercentage);

            // Write all sets to disk
            string itemSetRoot = "itemsets";
            string webDataRoot = "web";

            string webItemSetRoot = Path.Combine(webDataRoot, itemSetRoot);

            // Create web directory if it doesn't exist
            if (!Directory.Exists(webDataRoot))
                Directory.CreateDirectory(webDataRoot);

            // Clear old item sets
            if (Directory.Exists(webItemSetRoot))
                Directory.Delete(webItemSetRoot, true);

            Directory.CreateDirectory(webItemSetRoot);

            // Filter out sets we don't consider valid
            long minMatchCount = Math.Min(
                SetBuilderSettings.FilterMatchMinCount,
                (long)((double)pipeline.MatchCount * SetBuilderSettings.FilterMatchMinPercentage));

            var filteredSets = itemSets.Where(kvp =>
            {
                // Filter out item sets without a minimum percentage of matches
                if (kvp.Value.MatchCount < minMatchCount)
                    return false;

                // While smiteless-jungle might be viable, from the data this looks more like a
                // situation where the champion in question was roaming a lot, so the lane was
                // misidentified as jungle. We'll just exclude these for now.
                if (SetBuilderSettings.FilterExcludeNoSmiteJungle &&
                    kvp.Value.SetKey.Lane == Lane.Jungle && !kvp.Value.SetKey.HasSmite)
                    return false;

                // All other sets are fine
                return true;
            });

            // Group sets
            var groupedSets = filteredSets.GroupBy(kvp => kvp.Key.ChampionId);

            // Combine sets
            var smiteSpell = StaticDataStore.SummonerSpells.SummonerSpells["SummonerSmite"];

            var combinedSets = groupedSets.Select(g =>
            {
                // If there aren't two sets, or the two sets both do or don't have smite
                if (g.Count() != 2 ||
                    !(g.Any(set => set.Key.HasSmite) && g.Any(set => set.Key.HasSmite)))
                    return new { Key = g.Key, Sets = g.ToList() };

                var seta = g.ElementAt(0);
                var setb = g.ElementAt(1);

                var smiteset = seta.Key.HasSmite ? seta : setb;
                var nosmiteset = seta.Key.HasSmite ? setb : seta;

                // Mark set blocks as smite required or not required
                smiteset.Value.blocks.ForEach(block => block.showIfSummonerSpell = smiteSpell.Key);
                nosmiteset.Value.blocks.ForEach(block => block.hideIfSummonerSpell = smiteSpell.Key);

                // Combine into smite set
                smiteset.Value.MatchCount = Math.Max(smiteset.Value.MatchCount, nosmiteset.Value.MatchCount);
                smiteset.Value.blocks.AddRange(nosmiteset.Value.blocks);

                // Return combined block
                return new { Key = g.Key, Sets = Enumerable.Repeat(smiteset, 1).ToList() };
            }).ToList();

            // Generate names for item sets
            var setsWithNames = combinedSets.SelectMany(g =>
            {
                var champion = StaticDataStore.Champions.GetChampionById(g.Key);

                // Ensure the champion directoy exists
                string webpath = Path.Combine(itemSetRoot, champion.Key);
                string path = Path.Combine(webDataRoot, webpath);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                // If single item, just name it "Always"
                if (g.Sets.Count() == 1)
                {
                    return Enumerable.Repeat(new
                    {
                        Name = champion.Key + "_ProBuilds_" + "Always" + ".json",
                        WebPath = webpath,
                        FilePath = path,
                        Key = g.Sets.First().Key,
                        Set = g.Sets.First().Value
                    }, 1);
                }

                // Find differentiating fields
                bool diffHasSmite = g.Sets.Any(kvp => kvp.Key.HasSmite != g.Sets.First().Key.HasSmite);
                bool diffLane = g.Sets.Any(kvp => kvp.Key.Lane != g.Sets.First().Key.Lane);

                // Create name for each based on differentiating fields
                return g.Sets.Select(setkvp =>
                {
                    string name = "ProBuilds_" + champion.Key;

                    if (diffLane)
                        name += "_" +setkvp.Key.Lane.ToString();

                    if (diffHasSmite)
                        name += setkvp.Key.HasSmite ? "_Smite" : "_NoSmite";

                    name += ".json";

                    return new
                    {
                        Name = name,
                        WebPath = webpath,
                        FilePath = path,
                        Key = setkvp.Key,
                        Set = setkvp.Value
                    };
                });
            }).ToList();

            // Write item sets
            var setfiles = setsWithNames.AsParallel().WithDegreeOfParallelism(4).Select(set =>
            {
                string filename = set.Name;
                string file = Path.Combine(set.FilePath, filename);
                string setJson = JsonConvert.SerializeObject(set.Set);
                File.WriteAllText(file, setJson);

                return new { Key = set.Key, File = Path.Combine(set.WebPath, filename) };
            }).GroupBy(set => set.Key.ChampionId).ToDictionary(
                g => StaticDataStore.Champions.Keys[g.Key],
                g => g.ToList());


            // Write static data
            string championsfile = Path.Combine(webDataRoot, "champions.json");
            string itemsfile = Path.Combine(webDataRoot, "items.json");

            string championsjson = JsonConvert.SerializeObject(StaticDataStore.Champions);
            string itemsjson = JsonConvert.SerializeObject(StaticDataStore.Items);

            File.WriteAllText(championsfile, championsjson);
            File.WriteAllText(itemsfile, itemsjson);

            // Write item set manifest
            var manifest = new { root = itemSetRoot + Path.DirectorySeparatorChar, sets = setfiles };

            string manifestfile = Path.Combine(webDataRoot, "setmanifest.json");
            string manifestjson = JsonConvert.SerializeObject(manifest);

            File.WriteAllText(manifestfile, manifestjson);

            // Zip all item sets
            string zipSource = webItemSetRoot;
            string zipOutputFilename =  Path.Combine(webDataRoot, "allprosets.zip");
            if (File.Exists(zipOutputFilename))
                File.Delete(zipOutputFilename);

            ZipFile.CreateFromDirectory(zipSource, zipOutputFilename);

            // Serialize all purchase stats
            //string json = JsonConvert.SerializeObject(championPurchaseStats);

            // Complete
            Console.WriteLine();
            Console.WriteLine("Complete, press any key to exit");
            Console.ReadKey();
        }
    }
}