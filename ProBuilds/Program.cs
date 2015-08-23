using Newtonsoft.Json;
using RiotSharp;
using RiotSharp.LeagueEndpoint;
using RiotSharp.MatchEndpoint;
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
    class Program
    {
        static void Main(string[] args)
        {
            // Create query parameters
            // TODO: pass these in on command line, or in a settings file?
            RiotQuerySettings querySettings = new RiotQuerySettings(Region.na, Queue.RankedSolo5x5);

            // Parse api key from args
            if (args.Length == 0)
                return;

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
            StaticDataStore.Initialize(staticApi, querySettings);

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


            // Complete
            Console.WriteLine();
            Console.WriteLine("Complete, press any key to exit");
            Console.ReadKey();
        }
    }
}