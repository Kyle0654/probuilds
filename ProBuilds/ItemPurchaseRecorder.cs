using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public class ItemPurchaseRecorder : IMatchDetailProcessor
    {
        public int MaxDegreeOfParallelism { get { return 8; } }
        public int BoundedCapacity { get { return 128; } }

        public Task ConsumeMatchDetail(MatchDetail match)
        {
            return null;
        }
    }
}
