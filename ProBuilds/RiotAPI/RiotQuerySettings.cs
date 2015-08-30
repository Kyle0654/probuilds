using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RiotSharp;

namespace ProBuilds
{
    public class RiotQuerySettings
    {
        public Queue Queue { get; private set; }

        /// <summary>
        /// Whether or not new match data should be downloaded.
        /// </summary>
        /// <remarks>Will eventually be able to run analysis fully offline.</remarks>
        public bool NoDownload { get; set; }
        
        /// <summary>
        /// Shared query settings to use across queries.
        /// </summary>
        public RiotQuerySettings(
            Queue queue = Queue.RankedSolo5x5)
        {
            Queue = queue;

            NoDownload = false;
        }
    }
}
