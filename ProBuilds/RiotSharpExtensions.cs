using RiotSharp;
using RiotSharp.StaticDataEndpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    /// <summary>
    /// Extensions for RiotSharp
    /// </summary>
    static class RiotSharpExtensions
    {
        public static bool IsRetryable(this RiotSharpException ex)
        {
            return (ex.Message.StartsWith("429") || ex.Message.StartsWith("5"));
        }
    }
}
