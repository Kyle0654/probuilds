using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public interface IMatchDetailProcessor
    {
        int MaxDegreeOfParallelism { get; }
        int BoundedCapacity { get; }
        Task ConsumeMatchDetail(MatchDetail match);
    }
}
