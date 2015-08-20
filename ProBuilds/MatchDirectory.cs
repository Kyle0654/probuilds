using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    // TODO: Comment
    public static class MatchDirectory
    {
        static string MatchRoot = "matches";

        static MatchDirectory()
        {
            EnsureDirectories();
        }

        public static void EnsureDirectories()
        {
            if (!Directory.Exists(MatchRoot))
                Directory.CreateDirectory(MatchRoot);
        }

        private static string GetMatchPath(string matchVersion, long matchId)
        {
            RiotVersion version = new RiotVersion(matchVersion);
            string path = Path.Combine(MatchRoot, version.Major, version.Minor, version.Patch, matchId + ".json.gz");
            return path;
        }

        public static string GetMatchPath(MatchDetail match)
        {
            return GetMatchPath(match.MatchVersion, match.MatchId);
        }

        public static string GetMatchPath(MatchSummary match)
        {
            return GetMatchPath(match.MatchVersion, match.MatchId);
        }
    }
}
