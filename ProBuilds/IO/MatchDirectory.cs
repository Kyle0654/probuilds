using RiotSharp.MatchEndpoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds.IO
{
    /// <summary>
    /// Cache storage for matches, so we don't have to download them every time.
    /// </summary>
    public static class MatchDirectory
    {
        static string MatchRoot = "matches";
        static string MatchFileExtension = ".json.gz";

        static MatchDirectory()
        {
            EnsureDirectories();
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(MatchRoot))
                Directory.CreateDirectory(MatchRoot);
        }

        private static string GetMatchDirectory(RiotVersion version, bool includePatch = true)
        {
            if (includePatch)
                return Path.Combine(MatchRoot, version.Major, version.Minor, version.Patch);
            else
                return Path.Combine(MatchRoot, version.Major, version.Minor);
        }

        private static string GetMatchPath(string matchVersion, long matchId)
        {
            string dir = GetMatchDirectory(new RiotVersion(matchVersion));
            string path = Path.Combine(dir, matchId + MatchFileExtension);
            return path;
        }

        /// <summary>
        /// Get the path to a match file.
        /// </summary>
        public static string GetMatchPath(MatchDetail match)
        {
            return GetMatchPath(match.MatchVersion, match.MatchId);
        }

        /// <summary>
        /// Get the path to a match file.
        /// </summary>
        public static string GetMatchPath(MatchSummary match)
        {
            return GetMatchPath(match.MatchVersion, match.MatchId);
        }

        /// <summary>
        /// Provides a list of all match file paths.
        /// </summary>
        public static IEnumerable<string> GetAllMatchFiles()
        {
            string path = GetMatchDirectory(StaticDataStore.Version, false);
            if (!Directory.Exists(path))
                return Enumerable.Empty<string>();
            
            return Directory.EnumerateFiles(path, "*" + MatchFileExtension, SearchOption.AllDirectories);
        }

        /// <summary>
        /// Check if a match file exists.
        /// </summary>
        public static bool MatchFileExists(MatchSummary match)
        {
            string filename = GetMatchPath(match);
            return File.Exists(filename);
        }

        /// <summary>
        /// Save a match.
        /// </summary>
        public static void SaveMatch(MatchDetail match)
        {
            string filename = GetMatchPath(match);
            CompressedJson.WriteToFile(filename, match);
        }

        /// <summary>
        /// Load a match.
        /// </summary>
        public static MatchDetail LoadMatch(string filename)
        {
            return CompressedJson.ReadFromFile<MatchDetail>(filename);
        }

        /// <summary>
        /// Load a match.
        /// </summary>
        public static MatchDetail LoadMatch(MatchSummary match)
        {
            string filename = GetMatchPath(match);
            return CompressedJson.ReadFromFile<MatchDetail>(filename);
        }
    }
}
