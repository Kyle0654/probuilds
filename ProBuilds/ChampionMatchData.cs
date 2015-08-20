using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public class ChampionMatchData<T>
    {
        public int ChampionId { get; private set; }
        public ConcurrentBag<Tuple<long, T>> Matches = new ConcurrentBag<Tuple<long, T>>();

        public ChampionMatchData() {}

        public ChampionMatchData(int championId)
        {
            ChampionId = championId;
        }

        public void AddMatch(long matchId, T data)
        {
            Matches.Add(new Tuple<long, T>(matchId, data));
        }
    }

    public class ChampionMatchDataCollection<T>
    {
        private ConcurrentDictionary<int, ChampionMatchData<T>> championMatchData = new ConcurrentDictionary<int, ChampionMatchData<T>>();
        public ConcurrentDictionary<int, ChampionMatchData<T>> ChampionMatchData { get { return championMatchData; } }

        /// <summary>
        /// Create a match data from a champion id
        /// </summary>
        public ChampionMatchData<T> GetOrCreateMatchData(int championId)
        {
            ChampionMatchData<T> championMatches;
            if (!championMatchData.TryGetValue(championId, out championMatches))
            {
                // TODO: this is expensive, but a lot simpler than setting up all the 
                championMatches = new ChampionMatchData<T>(championId);
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
