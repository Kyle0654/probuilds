//using RiotSharp.MatchEndpoint;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ProBuilds
//{
//    // TODO: Comment
//    public static class ChampionPurchaseDirectory
//    {
//        static string ChampionPurchaseRoot = "championpurchases";
//        static string ChampionPurchaseFileExtension = ".json.gz";

//        static ChampionPurchaseDirectory()
//        {
//            EnsureDirectories();
//        }

//        private static void EnsureDirectories()
//        {
//            if (!Directory.Exists(ChampionPurchaseRoot))
//                Directory.CreateDirectory(ChampionPurchaseRoot);
//        }

//        private static string GetChampionDirectory(int championId)
//        {

//            if (!Directory.Exists)
//        }

//        private static string GetMatchPath(string matchVersion, long matchId)
//        {
//            string dir = GetMatchDirectory(new RiotVersion(matchVersion));
//            string path = Path.Combine(dir, matchId + ChampionPurchaseFileExtension);
//            return path;
//        }

//        /// <summary>
//        /// Get the path to a match file.
//        /// </summary>
//        public static string GetMatchPath(MatchDetail match)
//        {
//            return GetMatchPath(match.MatchVersion, match.MatchId);
//        }

//        /// <summary>
//        /// Get the path to a match file.
//        /// </summary>
//        public static string GetMatchPath(MatchSummary match)
//        {
//            return GetMatchPath(match.MatchVersion, match.MatchId);
//        }

//        /// <summary>
//        /// Provides a list of all match file paths.
//        /// </summary>
//        public static IEnumerable<string> GetAllMatchFiles()
//        {
//            string path = GetMatchDirectory(StaticDataStore.Version, false);
//            if (!Directory.Exists(path))
//                return Enumerable.Empty<string>();

//            return Directory.EnumerateFiles(path, "*" + ChampionPurchaseFileExtension, SearchOption.AllDirectories);
//        }

//        /// <summary>
//        /// Check if a match file exists.
//        /// </summary>
//        public static bool MatchFileExists(MatchSummary match)
//        {
//            string filename = GetMatchPath(match);
//            return File.Exists(filename);
//        }

//        /// <summary>
//        /// Save a match.
//        /// </summary>
//        public static void SaveMatch(MatchDetail match)
//        {
//            string filename = GetMatchPath(match);
//            CompressedJson.WriteToFile(filename, match);
//        }

//        /// <summary>
//        /// Load a match.
//        /// </summary>
//        public static MatchDetail LoadMatch(string filename)
//        {
//            return CompressedJson.ReadFromFile<MatchDetail>(filename);
//        }

//        /// <summary>
//        /// Load a match.
//        /// </summary>
//        public static MatchDetail LoadMatch(MatchSummary match)
//        {
//            string filename = GetMatchPath(match);
//            return CompressedJson.ReadFromFile<MatchDetail>(filename);
//        }
//    }
//}
