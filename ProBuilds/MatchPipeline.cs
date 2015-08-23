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
using System.Threading.Tasks.Dataflow;

namespace ProBuilds
{
    public class TestSynchronizer
    {
        public long Limit = 500; // Max matches stored on disk
        public long Count = 0;
    }

    /// <summary>
    /// Pipeline to query for and process matches.
    /// </summary>
    public class MatchPipeline
    {
        // Data flow blocks
        private BufferBlock<MatchDetail> MatchFileBufferBlock;
        private TransformBlock<MatchSummary, MatchDetail> ConsumeMatchBlock;
        private ActionBlock<MatchDetail> ConsumeMatchDetailBlock;

        // Processors (contain data flow blocks, along with some state)
        private PlayerMatchProducer playerMatchProducer;
        private IMatchDetailProcessor matchDetailProcessor;

        // Data for queries
        private RiotApi api;
        private RiotQuerySettings querySettings;
        private List<Queue> queryQueues = new List<Queue>();

        // Match concurrency (only download a match once)
        private ConcurrentDictionary<long, byte> processingMatches = new ConcurrentDictionary<long, byte>();

        // <TEST> test to limit the number of downloads
        private TestSynchronizer testSynchronizer = new TestSynchronizer();
        // </TEST>

        public MatchPipeline(RiotApi riotApi, RiotQuerySettings querySettings, IMatchDetailProcessor matchDetailProcessor)
        {
            api = riotApi;
            this.querySettings = querySettings;
            this.matchDetailProcessor = matchDetailProcessor;

            queryQueues.Add(querySettings.Queue);

            // Create match producer
            playerMatchProducer = new PlayerMatchProducer(api, querySettings, queryQueues, testSynchronizer);

            // Create blocks
            //MatchFileBufferBlock = new BufferBlock<MatchDetail>();
            ConsumeMatchBlock = new TransformBlock<MatchSummary, MatchDetail>(
                async match => await ConsumeMatch(match),
                new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 5 });
            ConsumeMatchDetailBlock = new ActionBlock<MatchDetail>(
                async match => await matchDetailProcessor.ConsumeMatchDetail(match),
                new ExecutionDataflowBlockOptions() {
                    MaxDegreeOfParallelism = matchDetailProcessor.MaxDegreeOfParallelism
                });

            // Link blocks
            playerMatchProducer.PlayerToMatchesBlock.LinkTo(ConsumeMatchBlock, new DataflowLinkOptions() { PropagateCompletion = false });
            //MatchFileBufferBlock.LinkTo(ConsumeMatchDetailBlock, new DataflowLinkOptions() { PropagateCompletion = false });
            ConsumeMatchBlock.LinkTo(ConsumeMatchDetailBlock, new DataflowLinkOptions() { PropagateCompletion = false }, match => match != null);
            ConsumeMatchBlock.LinkTo(DataflowBlock.NullTarget<MatchDetail>(), new DataflowLinkOptions() { PropagateCompletion = false });
        }

        /// <summary>
        /// Start producing players.
        /// </summary>
        public void Process()
        {
            // Load all downloaded matches
            LoadMatchFiles();

            // Produce all players
            playerMatchProducer.Begin();

            // Waits
            playerMatchProducer.PlayerToMatchesBlock.Completion.Wait();
            var pmstatus = playerMatchProducer.PlayerToMatchesBlock.Completion.Status;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished processing players into matches");
            Console.ResetColor();

            ConsumeMatchBlock.Complete();
            var consumeStatus = ConsumeMatchBlock.Completion.Status;
            //var filebufferstatus = MatchFileBufferBlock.Completion.Status;
            //Task.WaitAll(ConsumeMatchBlock.Completion, MatchFileBufferBlock.Completion);
            ConsumeMatchBlock.Completion.Wait();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished Pulling matches");
            Console.ResetColor();

            ConsumeMatchDetailBlock.Complete();
            ConsumeMatchDetailBlock.Completion.Wait();
        }

        public void LoadMatchFiles()
        {
            var matchFiles = MatchDirectory.GetAllMatchFiles();

            // Start the counter at how many files we already have
            int matchFileCount = matchFiles.Count();
            testSynchronizer.Count = matchFileCount;
            Console.WriteLine("Match Files Cached: {0}", matchFileCount);

            // Load match files
            matchFiles.AsParallel().WithDegreeOfParallelism(4).ForAll(filename =>
            {
                MatchDetail match = MatchDirectory.LoadMatch(filename);
                if (match == null)
                {
                    // Match file has an error, delete the cached match file
                    File.Delete(filename);
                }
                else
                {
                    //MatchFileBufferBlock.Post(match);
                    ConsumeMatchDetailBlock.Post(match);
                }
            });

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished loading match files");
            Console.ResetColor();

            //MatchFileBufferBlock.Complete();
        }

        private bool TryLockMatch(long matchId)
        {
            return processingMatches.TryAdd(matchId, 1);
        }

        private void TryUnlockMatch(long matchId)
        {
            byte dummyVal;
            processingMatches.TryRemove(matchId, out dummyVal);
        }

        /// <summary>
        /// Process a match.
        /// </summary>
        private async Task<MatchDetail> ConsumeMatch(MatchSummary match)
        {
            long matchId = match.MatchId;

            // Try to mark the match as being processed
            if (!TryLockMatch(matchId))
            {
                // Another thread has already started processing this match, we don't need to do anything
                return null;
            }

            // Get the disk path and version of the match
            string matchPath = MatchDirectory.GetMatchPath(match);
            RiotVersion matchVersion = new RiotVersion(match.MatchVersion);

            // Check if the match version is older than the current realm version
            if (!StaticDataStore.Version.IsSamePatch(matchVersion))
            {
                TryUnlockMatch(matchId);
                return null;
            }

            // Check if the match exists on disk
            if (MatchDirectory.MatchFileExists(match))
            {
                // Match file loading will handle this
                TryUnlockMatch(matchId);
                return null;
            }

            // <TEST> download limiting
            long count = Interlocked.Read(ref testSynchronizer.Count);
            Interlocked.Increment(ref testSynchronizer.Count);
            if (count >= testSynchronizer.Limit)
                return null;
            // </TEST>

            MatchDetail matchData = null;

            try
            {
                // Get the match with full timeline data
                matchData = await api.GetMatchAsync(querySettings.Region, matchId, true);
                if (matchData == null)
                    throw new RiotSharpException("Null match: " + matchId);

                // Save it to disk
                MatchDirectory.SaveMatch(matchData);

                Console.WriteLine(count);
            }
            catch (RiotSharpException ex)
            {
                // Don't do anything with the exception yet
                // TODO: log exceptions

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(count + ": Error: " + ex.Message);
                Console.ResetColor();
            }

            // Remove the match from current downloads
            TryUnlockMatch(matchId);

            return matchData;
        }
    }
}
