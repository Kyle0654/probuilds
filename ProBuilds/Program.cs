using Newtonsoft.Json;
using ProBuilds.BuildPath;
using ProBuilds.Match;
using ProBuilds.Pipeline;
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

            // Generate item sets
            Dictionary<PurchaseSetKey, ItemSet> itemSets = ItemSetGenerator.generateAll(championPurchaseStats);

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
                    return new { Key = g.Key, Sets = g.ToList(), HasOtherLane = false, OtherLane = Lane.Bot };

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
                return new { Key = g.Key, Sets = Enumerable.Repeat(smiteset, 1).ToList(), HasOtherLane = true, OtherLane = nosmiteset.Key.Lane };
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

                // Find differentiating fields
                bool diffHasSmite = g.Sets.Any(kvp => kvp.Key.HasSmite != g.Sets.First().Key.HasSmite);
                bool diffLane = g.Sets.Any(kvp => kvp.Key.Lane != g.Sets.First().Key.Lane);

                // Create name for each based on differentiating fields
                return g.Sets.Select(setkvp =>
                {
                    string filename = "ProBuilds_" + champion.Key;
                    string title = "";

                    // Always handle combined sets specially
                    if (g.Sets.Count == 1 && g.HasOtherLane == true)
                    {
                        title += g.OtherLane.ToString() + " / " + g.Sets.First().Key.Lane.ToString();
                        filename += "_" + g.OtherLane.ToString() + "_" + g.Sets.First().Key.Lane.ToString();
                    }
                    else
                    {
                        // We always write out jungle
                        // We always write out the lane if it has smite (to differentiate it from jungle)
                        if (g.Sets.Count == 1 ||
                            diffLane ||
                            (setkvp.Key.Lane == Lane.Jungle && !setkvp.Value.blocks.Any(block => block.showIfSummonerSpell != "")) ||
                            setkvp.Key.HasSmite)
                        {
                            title += setkvp.Key.Lane.ToString();
                            filename += "_" + setkvp.Key.Lane.ToString();
                        }

                        if (diffHasSmite)
                        {
                            // Add smite information to title if jungling without smite, or taking smite without jungling
                            if ((setkvp.Key.HasSmite && setkvp.Key.Lane != Lane.Jungle) ||
                                (!setkvp.Key.HasSmite && setkvp.Key.Lane == Lane.Jungle))
                            {
                                title += setkvp.Key.HasSmite ? " with Smite" : " without Smite";
                            }

                            filename += setkvp.Key.HasSmite ? "_Smite" : "_NoSmite";
                        }
                    }

                    // Set the title
                    setkvp.Value.title = title;

                    filename += ".json";

                    return new
                    {
                        Name = filename,
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

                return new { Key = set.Key, File = Path.Combine(set.WebPath, filename).Replace('\\', '/'), Title = set.Set.title };
            }).GroupBy(set => set.Key.ChampionId).ToDictionary(
                g => StaticDataStore.Champions.Keys[g.Key],
                g => g.ToList());


            // Write static data
            string championsfile = Path.Combine(webDataRoot, "champions.json");
            string itemsfile = Path.Combine(webDataRoot, "items.json");
            string summonerspellsfile = Path.Combine(webDataRoot, "summonerspells.json");

            string championsjson = JsonConvert.SerializeObject(StaticDataStore.Champions);
            string itemsjson = JsonConvert.SerializeObject(StaticDataStore.Items);
            string summonerspellsjson = JsonConvert.SerializeObject(StaticDataStore.SummonerSpells);

            File.WriteAllText(championsfile, championsjson);
            File.WriteAllText(itemsfile, itemsjson);
            File.WriteAllText(summonerspellsfile, summonerspellsjson);

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