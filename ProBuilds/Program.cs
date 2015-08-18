using Newtonsoft.Json;
using RiotSharp;
using RiotSharp.LeagueEndpoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public struct RiotVersion
    {
        public enum Tolerance
        {
            Major,
            Minor,
            Patch,
            SubPatch
        }

        public string Major;
        public string Minor;
        public string Patch;
        public string SubPatch;

        private string version;

        public RiotVersion(string version)
        {
            this.version = version;

            if (string.IsNullOrWhiteSpace(version))
            {
                Major = Minor = Patch = SubPatch = null;
                return;
            }

            string[] splits = version.Split('.');
            Major = splits.Length > 0 ? splits[0] : null;
            Minor = splits.Length > 1 ? splits[1] : null;
            Patch = splits.Length > 2 ? splits[2] : null;
            SubPatch = splits.Length > 3 ? splits[3] : null;
        }

        public bool IsSamePatch(RiotVersion other, Tolerance tolerance = Tolerance.Minor)
        {
            if (Major != other.Major) return false;
            if (tolerance == Tolerance.Major) return true;
            if (Minor != other.Minor) return false;
            if (tolerance == Tolerance.Minor) return true;
            if (Patch != other.Patch) return false;
            if (tolerance == Tolerance.Patch) return false;
            return (SubPatch != other.SubPatch);
        }

        public override string ToString()
        {
            return version;
        }
    }

    public class PlayerDirectory
    {
        static string PathRoot = "stats";
        static string MatchPathName = "matches";

        public string PlayerPath { get; private set; }
        public string MatchRoot { get; private set; }

        public PlayerDirectory(LeagueEntry entry)
        {
            PlayerPath = Path.Combine(PathRoot, entry.PlayerOrTeamId.ToString());
            MatchRoot = Path.Combine(PlayerPath, MatchPathName);
        }

        public void EnsureDirectories()
        {
            if (!Directory.Exists(PlayerPath))
                Directory.CreateDirectory(PlayerPath);

            if (!Directory.Exists(MatchRoot))
                Directory.CreateDirectory(MatchRoot);
        }
    }

    class Program
    {
        // TODO: Place this in a settings file
        static string apiKey = "";
        static Region region = Region.na;

        static void Main(string[] args)
        {
            if (args.Length == 0)
                return;

            apiKey = args[0];

            StaticRiotApi staticApi = StaticRiotApi.GetInstance(apiKey);

            // Get static data
            var champions = staticApi.GetChampions(region);
            var items = staticApi.GetItems(region);
            var version = staticApi.GetVersions(region);

            // Get challenger list
            RiotApi api = RiotApi.GetInstance(apiKey);
            var challenger = api.GetChallengerLeague(region, Queue.RankedSolo5x5);
            var count = challenger.Entries.Count();

            Dictionary<int, RiotSharp.StaticDataEndpoint.ItemStatic> itemDictionary;
            itemDictionary = items.Items.ToDictionary(kvp => kvp.Value.Id, kvp => kvp.Value);


            challenger.Entries.ForEach(player =>
            {
                string json = JsonConvert.SerializeObject(player, Formatting.Indented);

                PlayerDirectory dir = new PlayerDirectory(player);
                dir.EnsureDirectories();

                string playerFilename = Path.Combine(dir.PlayerPath, "player.json");
                File.WriteAllText(playerFilename, json);
            });

            //// NOTE: THIS IS INTENTIONALLY TRYING TO OVER-RATE LIMIT TO SEE WHAT THE BEHAVIOR IS LIKE
            //// NOTE: Behavior seems to be that it will block until rate limit is released.
            //for (int i = 0; i < count && i < 20; ++i)
            //{
            //    try
            //    {
            //        var matches = api.GetMatchHistory(region, long.Parse(challenger.Entries[0].PlayerOrTeamId));
            //    }
            //    catch (RiotSharpException ex)
            //    {
            //        System.Diagnostics.Debugger.Break();
            //    }
            //}
            List<Queue> queues = new List<Queue>(new Queue[] { Queue.RankedSolo5x5 });
            RiotVersion itemVersion = new RiotVersion(items.Version);

            // Get all matches for each player
            foreach (var player in challenger.Entries)
            {
                var matches = api.GetMatchHistory(region, long.Parse(challenger.Entries[0].PlayerOrTeamId),
                    rankedQueues: queues);

                ConsoleColor consoleColorDefault = Console.ForegroundColor;
                matches.ForEach(m =>
                {
                    RiotVersion matchVersion = new RiotVersion(m.MatchVersion);

                    if (itemVersion.IsSamePatch(matchVersion))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    Console.WriteLine(m.MatchId + ": " + m.MatchVersion);

                    Console.ForegroundColor = consoleColorDefault;
                });

                var matchVersions = matches.Select(m => new RiotVersion(m.MatchVersion)).ToList();

                var currentMatches = matches.Where(m => itemVersion.IsSamePatch(new RiotVersion(m.MatchVersion))).ToList();

                var matchid = matches[0].MatchId;
                //var match = api.GetMatch(region, matchid, true); // match with full timeline data
                //var events = match.Timeline.Frames.Where(f => f.Events != null).SelectMany(f => f.Events).ToList();

                //var itemPurchases = events.Where(e => e.EventType == RiotSharp.MatchEndpoint.EventType.ItemPurchased).Select(e => new {
                //    ItemId = e.ItemId,
                //    ItemName = itemDictionary.ContainsKey(e.ItemId) ? itemDictionary[e.ItemId].Name : null, // TODO: handle old match data (w.r.t. item data) - probably just ignore games older than current patch
                //    Timestamp = e.Timestamp,
                //    ParticipantId = e.ParticipantId
                //}).ToList();
            }
        }
    }
}
