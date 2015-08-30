using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    /// <summary>
    /// A sample class to count wins per champion.
    /// </summary>
    public class ChampionWinCounter : IMatchDetailProcessor
    {
        public class ChampionWinData
        {
            public long Wins;
            public long Matches;
            public long Losses { get { return Matches - Wins; } }

            public void Increment(bool isWinner)
            {
                if (isWinner)
                    ++Wins;

                ++Matches;
            }
        }

        public int MaxDegreeOfParallelism { get { return 16; } }

        public ConcurrentDictionary<int, ChampionWinData> ChampionWinCount { get; private set; }

        public ChampionWinCounter()
        {
            ChampionWinCount = new ConcurrentDictionary<int, ChampionWinData>();
        }

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

                ChampionWinCount.AddOrUpdate(championId,
                    id => { return new ChampionWinData() { Wins = isWinner ? 1 : 0, Matches = 1 }; },
                    (id, winData) => { winData.Increment(isWinner); return winData; }
                );
            });
        }
    }
}
