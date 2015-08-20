﻿using RiotSharp;
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
    public class MatchPipeline
    {
        private BufferBlock<LeagueEntry> PlayerBufferBlock;
        private TransformManyBlock<LeagueEntry, MatchSummary> PlayerToMatchesBlock;
        private TransformBlock<MatchSummary, MatchDetail> ConsumeMatchBlock;
        private ActionBlock<MatchDetail> ConsumeMatchDetailBlock;
        private IDataflowBlock LastBlock;

        private RiotApi api;
        private RiotQuerySettings querySettings;
        private List<Queue> queryQueues = new List<Queue>();

        private ConcurrentDictionary<long, byte> processingMatches = new ConcurrentDictionary<long, byte>();

        private IMatchDetailProcessor matchDetailProcessor;

        private long testLimit = 500;
        private long testCount = 0;

        public MatchPipeline(RiotApi riotApi, RiotQuerySettings querySettings, IMatchDetailProcessor matchDetailProcessor)
        {
            api = riotApi;
            this.querySettings = querySettings;
            this.matchDetailProcessor = matchDetailProcessor;

            queryQueues.Add(querySettings.Queue);

            // Create blocks
            PlayerBufferBlock = new BufferBlock<LeagueEntry>();
            PlayerToMatchesBlock = new TransformManyBlock<LeagueEntry, MatchSummary>(
                async player => await ConsumePlayerAsync(player),
                new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 2 });
            ConsumeMatchBlock = new TransformBlock<MatchSummary, MatchDetail>(
                async match => await ConsumeMatch(match),
                new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 5 });
            ConsumeMatchDetailBlock = new ActionBlock<MatchDetail>(
                async match => await matchDetailProcessor.ConsumeMatchDetail(match),
                new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = matchDetailProcessor.MaxDegreeOfParallelism });

            // Link blocks
            PlayerBufferBlock.LinkTo(PlayerToMatchesBlock);
            PlayerToMatchesBlock.LinkTo(ConsumeMatchBlock);
            ConsumeMatchBlock.LinkTo(ConsumeMatchDetailBlock, match => match != null);
            ConsumeMatchBlock.LinkTo(DataflowBlock.NullTarget<MatchDetail>());

            // Hook completion continuations
            PlayerBufferBlock.Completion.ContinueWith(t => PlayerToMatchesBlock.Complete());
            PlayerToMatchesBlock.Completion.ContinueWith(t => ConsumeMatchBlock.Complete());
            ConsumeMatchBlock.Completion.ContinueWith(t => ConsumeMatchDetailBlock.Complete());

            // Mark the last block in the chain to make iterating easier
            LastBlock = ConsumeMatchDetailBlock;
        }

        /// <summary>
        /// Start producing players.
        /// </summary>
        public void Process()
        {
            // Produce all players
            ProducePlayers();

            // Wait for completion
            PlayerBufferBlock.Complete();
            LastBlock.Completion.Wait();
        }

        /// <summary>
        /// Produce players to process.
        /// </summary>
        private void ProducePlayers()
        {
            var players = api.GetChallengerLeague(querySettings.Region, querySettings.Queue);
            players.Entries.ForEach(player => {
                PlayerBufferBlock.Post(player);
            });
        }

        /// <summary>
        /// Consume players and list matches per player.
        /// </summary>
        private async Task<IEnumerable<MatchSummary>> ConsumePlayerAsync(LeagueEntry player)
        {
            // <TEST> download limiting
            long count = Interlocked.Read(ref testCount);
            if (count >= testLimit)
                return Enumerable.Empty<MatchSummary>();
            // </TEST>

            // Get player id
            long playerId = long.Parse(player.PlayerOrTeamId);

            // TODO: store data on player (last seen match) to limit queries

            // Get player match list
            // TODO: use the new match history api
            var matches = await api.GetMatchHistoryAsync(querySettings.Region, playerId,
                rankedQueues: queryQueues);

            // TODO: paginate until we reach matches we've already seen, or that are too old to process

            return matches;
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

            // Don't re-download if:
            // the match version is older than the current realm version, or
            // the match exists on disk already
            if (!StaticDataStore.Version.IsSamePatch(matchVersion))
            {
                TryUnlockMatch(matchId);
                return null;
            }

            if (File.Exists(matchPath))
            {
                TryUnlockMatch(matchId);
                MatchDetail loadedMatchData = CompressedJson.ReadFromFile<MatchDetail>(matchPath);
                return loadedMatchData;
            }

            // <TEST> download limiting
            long count = Interlocked.Read(ref testCount);
            Interlocked.Increment(ref testCount);
            if (count >= testLimit)
                return null;
            // </TEST>

            MatchDetail matchData = null;

            try
            {
                // Get the match with full timeline data
                matchData = await api.GetMatchAsync(querySettings.Region, matchId, true);
                if (matchData == null)
                    throw new RiotSharpException("Null match: " + matchId);

                CompressedJson.WriteToFile(matchPath, matchData);

                Console.WriteLine(count);
            }
            catch (RiotSharpException ex)
            {
                // Don't do anything with the exception yet
                // TODO: log exceptions

                ConsoleColor foreColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(count + ": Error: " + ex.Message);
                Console.ForegroundColor = foreColor;
            }

            // Remove the match from current downloads
            TryUnlockMatch(matchId);

            return matchData;
        }
    }
}
