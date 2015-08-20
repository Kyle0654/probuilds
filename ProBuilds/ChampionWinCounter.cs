using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public class ChampionMatchWinData
    {
        public int ChampionId;
        public ConcurrentBag<Tuple<long, bool>> MatchIds = new ConcurrentBag<Tuple<long, bool>>();

        public ChampionMatchWinData(int championId)
        {
            ChampionId = championId;
        }

        public void AddMatch(long matchId, bool isWinner)
        {
            MatchIds.Add(new Tuple<long, bool>(matchId, isWinner));
        }
    }

    public class ChampionWinCounter : IMatchDetailProcessor
    {
        public int MaxDegreeOfParallelism { get { return 8; } }

        /// <summary>
        /// Data about champions from processed matches.
        /// </summary>
        public ConcurrentDictionary<int, ChampionMatchWinData> ChampionMatchData { get { return championMatchData; } }

        private ConcurrentDictionary<int, ChampionMatchWinData> championMatchData = new ConcurrentDictionary<int, ChampionMatchWinData>();

        /// <summary>
        /// Consume a match.
        /// </summary>
        public async Task ConsumeMatchDetail(RiotSharp.MatchEndpoint.MatchDetail match)
        {
            long matchId = match.MatchId;
            Console.WriteLine("Processing match " + matchId);

            match.Participants.ForEach(participant =>
            {
                int championId = participant.ChampionId;
                bool isWinner = match.Teams.FirstOrDefault(t => participant.TeamId == t.TeamId).Winner;

                ChampionMatchWinData championMatches = GetOrCreateMatchData(championId);
                championMatches.AddMatch(matchId, isWinner);
            });
        }

        /// <summary>
        /// Create a match win data from a champion id
        /// </summary>
        /// <param name="championId"></param>
        /// <returns></returns>
        private ChampionMatchWinData GetOrCreateMatchData(int championId)
        {
            ChampionMatchWinData championMatches;
            if (!championMatchData.TryGetValue(championId, out championMatches))
            {
                championMatches = new ChampionMatchWinData(championId);
                if (!championMatchData.TryAdd(championId, championMatches))
                {
                    // Two threads tried to add a champion match data at the same time
                    championMatchData.TryGetValue(championId, out championMatches);
                }
            }
            return championMatches;
        }
    }
}
