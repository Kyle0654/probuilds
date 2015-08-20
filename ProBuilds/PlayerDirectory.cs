using RiotSharp.LeagueEndpoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    // TODO: Comment
    public class PlayerDirectory
    {
        static string PathRoot = "stats";
        static string MatchPathName = "matches";

        public string PlayerPath { get; private set; }
        public string MatchRoot { get; private set; }

        public PlayerDirectory(LeagueEntry entry)
        {
            PlayerPath = Path.Combine(PathRoot, entry.PlayerOrTeamId.ToString());
            MatchRoot = Path.Combine(PlayerPath, MatchPathName);
        }

        public void EnsureDirectories()
        {
            if (!Directory.Exists(PlayerPath))
                Directory.CreateDirectory(PlayerPath);

            if (!Directory.Exists(MatchRoot))
                Directory.CreateDirectory(MatchRoot);
        }
    }
}
