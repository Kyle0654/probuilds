using Newtonsoft.Json;
using RiotSharp.LeagueEndpoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    /// <summary>
    /// All data we store about players (currently pretty minimal)
    /// </summary>
    public class PlayerData
    {
        public string PlayerId;
        public long LatestMatchId = -1;

        public PlayerData() {}

        public PlayerData(string playerId)
        {
            PlayerId = playerId;
        }
    }

    // TODO: Comment
    public static class PlayerDirectory
    {
        static string PlayerRoot = "players";

        static PlayerDirectory()
        {
            EnsureDirectories();
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PlayerRoot))
                Directory.CreateDirectory(PlayerRoot);
        }

        private static string GetPlayerFilename(LeagueEntry entry)
        {
            // We store minimal information here, so no need to compress
            return Path.Combine(PlayerRoot, entry.PlayerOrTeamId + ".json");
        }

        /// <summary>
        /// Gets player data.
        /// </summary>
        public static PlayerData GetPlayerData(LeagueEntry entry)
        {
            string filename = GetPlayerFilename(entry);
            if (!File.Exists(filename))
            {
                PlayerData data = new PlayerData(entry.PlayerOrTeamId);
                SetPlayerData(entry, data);
                return data;
            }
            else
            {
                string json = File.ReadAllText(filename);
                PlayerData data = JsonConvert.DeserializeObject<PlayerData>(json);
                return data;
            }
        }

        /// <summary>
        /// Stores player data.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="playerData"></param>
        public static void SetPlayerData(LeagueEntry entry, PlayerData playerData)
        {
            string filename = GetPlayerFilename(entry);
            string json = JsonConvert.SerializeObject(playerData, Formatting.Indented);
            File.WriteAllText(filename, json);
        }
    }
}
