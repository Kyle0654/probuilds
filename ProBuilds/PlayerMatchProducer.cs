using RiotSharp;
using RiotSharp.LeagueEndpoint;
using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ProBuilds
{
    public class PlayerMatchProducer
    {
        static int MaxMatchesPerQuery = 15;

        private BufferBlock<LeagueEntry> PlayerBufferBlock;
        public TransformManyBlock<LeagueEntry, MatchSummary> PlayerToMatchesBlock { get; private set; }

        private RiotApi api;
        private RiotQuerySettings querySettings;
        private List<Queue> queryQueues;

        private TestSynchronizer testSynchronizer;

        public PlayerMatchProducer(RiotApi riotApi, RiotQuerySettings querySettings, List<Queue> queryQueues, TestSynchronizer testSynchronizer)
        {
            api = riotApi;
            this.querySettings = querySettings;
            this.queryQueues = queryQueues;
            this.testSynchronizer = testSynchronizer;

            // Create blocks
            PlayerBufferBlock = new BufferBlock<LeagueEntry>();
            PlayerToMatchesBlock = new TransformManyBlock<LeagueEntry, MatchSummary>(
                async player => await ConsumePlayerAsync(player),
                new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 2 });

            // Link blocks
            PlayerBufferBlock.LinkTo(PlayerToMatchesBlock, new DataflowLinkOptions() { PropagateCompletion = true });
        }

        /// <summary>
        /// Begin producing players.
        /// </summary>
        public void Begin()
        {
            var playersChallenger = api.GetChallengerLeague(querySettings.Region, querySettings.Queue);
            var playersMaster = api.GetMasterLeague(querySettings.Region, querySettings.Queue);
            var playersAllPro = playersChallenger.Entries.Concat(playersMaster.Entries).ToList();

            playersAllPro.ForEach(player =>
            {
                PlayerBufferBlock.Post(player);
            });

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished queueing players to process");
            Console.ResetColor();

            PlayerBufferBlock.Complete();
        }

        /// <summary>
        /// Consume players and list matches per player.
        /// </summary>
        private async Task<IEnumerable<MatchSummary>> ConsumePlayerAsync(LeagueEntry player)
        {
            // <TEST> download limiting
            long count = Interlocked.Read(ref testSynchronizer.Count);
            if (count >= testSynchronizer.Limit)
                return Enumerable.Empty<MatchSummary>();
            // </TEST>

            // Get player id
            long playerId = long.Parse(player.PlayerOrTeamId);

            // Get player data
            PlayerData playerData = PlayerDirectory.GetPlayerData(player);

            // Retry a number of times to handle server-side rate limit errors
            int retriesLeft = 3;

            // Get player match list, looping through pages until reaching a match we've seen for this player, or a match from an old patch
            // TODO: use the new match history api
            int startId = 0;
            int endId = MaxMatchesPerQuery - 1;

            List<MatchSummary> matches = new List<MatchSummary>();
            bool oldMatches = false;
            while (!oldMatches)
            {
                try
                {
                    var newmatches = await api.GetMatchHistoryAsync(querySettings.Region, playerId,
                        rankedQueues: queryQueues,
                        beginIndex: startId,
                        endIndex: endId);

                    // Prepare for next query
                    startId += MaxMatchesPerQuery;
                    endId += MaxMatchesPerQuery;

                    // Filter out matches that aren't on the current patch
                    newmatches = newmatches.Where(m => StaticDataStore.Version.IsSamePatch(new RiotVersion(m.MatchVersion))).ToList();

                    // Check if we've seen any of these matches yet (if so, that's our marker to stop querying)
                    int foundMatchId = newmatches.FindIndex(m => m.MatchId == playerData.LatestMatchId);
                    if (foundMatchId != -1)
                    {
                        newmatches = newmatches.GetRange(0, foundMatchId).ToList();
                        oldMatches = true;
                    }

                    matches.AddRange(newmatches);

                    // If we filtered any matches out, we're past matches on the current patch
                    if (newmatches.Count < MaxMatchesPerQuery)
                        oldMatches = true;
                }
                catch (RiotSharpException ex)
                {
                    if (ex.IsRetryable() && retriesLeft > 0)
                    {
                        // NOTE: This server-side rate handler is a bit rough, but I don't want to change RiotSharp too much unless I get more time.
                        //       Ideally, the original error code would be attached to the RiotSharp exception so the handler could decide how to deal
                        //       with it based on the status code. Unfortunately, it's not at this time - I'll clean it up in RiotSharp if I have time.
                        //
                        //       The error seems to be server-side, as there's no retry-after headers of any sort attached to the response. It should
                        //       be safe to just retry after waiting a bit. I'm also retrying 500-series errors, since they seem to be recoverable.
                        --retriesLeft;

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Server error getting matches for player {0} ({1} retries left)", player.PlayerOrTeamName, retriesLeft);
                        Console.ResetColor();

                        // Wait half a second
                        Thread.Sleep(500);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error getting matches for player {0}: {1}", player.PlayerOrTeamName, ex.Message);
                        Console.ResetColor();
                        oldMatches = true;
                    }
                }
            }

            // Store last seen match for this player
            if (matches.Count > 0)
            {
                playerData.LatestMatchId = matches[0].MatchId;
                PlayerDirectory.SetPlayerData(player, playerData);
            }

            return matches;
        }
    }
}
