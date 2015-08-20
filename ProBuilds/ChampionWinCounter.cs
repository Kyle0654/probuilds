using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public class ChampionWinCounter : IMatchDetailProcessor
    {
        public int BoundedCapacity { get { return 128; } }
        public int MaxDegreeOfParallelism { get { return 8; } }

        public ChampionMatchDataCollection<bool> ChampionMatchData { get; private set; }

        public ChampionWinCounter()
        {
            ChampionMatchData = new ChampionMatchDataCollection<bool>();
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

                ChampionMatchData<bool> championMatches = ChampionMatchData.GetOrCreateMatchData(championId);
                championMatches.AddMatch(matchId, isWinner);
            });
        }
    }
}
