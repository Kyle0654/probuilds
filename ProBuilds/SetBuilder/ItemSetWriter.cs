using Newtonsoft.Json;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.IO;
using ProBuilds.BuildPath;
using RiotSharp.StaticDataEndpoint;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Serialization;
using ProBuilds.SetBuilder;
using ProBuilds.IO;
using ProBuilds.Extensions;

namespace ProBuilds.SetBuilder
{
    static class ItemSetWriter
    {
        /// <summary>
        /// Sub path to global item sets
        /// </summary>
        private const string GlobalSubPath = "Config\\Global\\Recommended\\";

        /// <summary>
        /// Sub path to champion item sets (use string.Format to replace {0} with champion key)
        /// </summary>
        private const string ChampionSubPath = "Config\\Champions\\{0}\\Recommended\\";

        /// <summary>
        /// Full file path to league of legends install directory
        /// </summary>
        private static string LeagueOfLegendsPath = "C:\\Riot Games\\League of Legends\\";

        /// <summary>
        /// File extention to use for the file
        /// </summary>
        private const string FileExtention = ".json";

        /// <summary>
        /// Gets the full directory path to where the item sets should go or are stored
        /// </summary>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <returns>Full path to config</returns>
        private static string getItemSetDirectory(string championKey = "")
        {
            if (championKey == "")
                return Path.Combine(LeagueOfLegendsPath, GlobalSubPath);
            else
                return Path.Combine(LeagueOfLegendsPath, string.Format(ChampionSubPath, championKey));
        }
        
        /// <summary>
        /// Set the full directory path to the league of legends installation
        /// </summary>
        /// <param name="fullPath">Full directory path to the league of legends installation</param>
        public static void setLeagueOfLegendsPath(string fullPath = "C:\\Riot Games\\League of Legends")
        {
            LeagueOfLegendsPath = fullPath;
        }

        /// <summary>
        /// Get the currently set full directory path to the league of legends installation
        /// </summary>
        /// <returns>Currently set full directory path to the league of legends installation</returns>
        public static string getLeagueOfLegendsPath()
        {
            return LeagueOfLegendsPath;
        }

        /// <summary>
        /// Look through the installed programs registry values and find where league of legends is installed
        /// </summary>
        public static void findLeagueOfLegendsPath()
        {
            foreach(var item in Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall").GetSubKeyNames())
            {
                object programName = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + item).GetValue("DisplayName");
                if(string.Equals(programName, "League of Legends"))
                {
                    object programLocation = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + item).GetValue("InstallLocation");
                    LeagueOfLegendsPath = programLocation.ToString();
                    break;
                }
            }
        }

        /// <summary>
        /// Make sure a directory exists and if not create it
        /// </summary>
        /// <param name="path">directory path to ensure</param>
        private static void ensureDirectory(string path)
        {
            string dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
        }

        /// <summary>
        /// Crate the item set file name based on our naming convention
        /// </summary>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <param name="map">Map type this item is set for</param>
        /// <param name="name">Custom name added to file name</param>
        /// <returns>Name for file including file extention</returns>
        public static string getItemSetFileName(string championKey, ItemSet.Map map, string name)
        {
            //Convert the enum value to the EnumMember string
            var atr = map.GetAttribute<EnumMemberAttribute>();
            string mapAbreviation = atr.Value.ToString();

            return ItemSetNaming.ToolName + championKey + mapAbreviation + name + FileExtention;
        }

        /// <summary>
        /// Write out the item set to a json file in the league of legends item set directory for the given champion key
        /// If directory doesn't exist it will be created so be careful
        /// </summary>
        /// <param name="itemSet">Item set to write out to a json file</param>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <param name="name">Unique name to append to this ItemSet</param>
        /// <param name="writeExtraFields">True to write out any fields taged as extra, false to not</param>
        /// <param name="subDir">Sub directory to write to, if empty use League of Legends directory</param>
        /// <returns>True if item set doesn't exists and was written, false if item set exists already</returns>
        public static bool writeOutItemSet(ItemSet itemSet, string championKey = "", string name = "", bool writeExtraFields = true, string subDir = "")
        {
            //Setup our custom serialization settings
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            if (!writeExtraFields)
                settings.ContractResolver = new ExcludeMetadataFieldsContractResolver();

            //Convert our item set to json
            string JSON = JsonConvert.SerializeObject(itemSet, settings);

            //Get the directory we are going to write out to
            string itemSetDir = (String.IsNullOrEmpty(subDir)) ? getItemSetDirectory(championKey) : Path.Combine(subDir, championKey == "" ? "Global" : championKey) + Path.DirectorySeparatorChar;

            //Make sure the directory exists
            ensureDirectory(itemSetDir);

            //Create our unique file name for the system
            string fileName = Path.Combine(itemSetDir, getItemSetFileName(championKey, itemSet.map, name));

            //Check if we already have this file
            if (File.Exists(fileName))
                return false;

            //Write out the file
            File.WriteAllText(fileName, JSON);

            return true;
        }

        /// <summary>
        /// Read in an item set from disk
        /// </summary>
        /// <param name="championKey">Key value for the champion you want the item set directory for or empty if global</param>
        /// <param name="map">Map this item set is for (used in naming convention)</param>
        /// <param name="name">Name for this item set file (used in naming convention)</param>
        /// <param name="subDir">Sub directory to read from, if empty use League of Legends directory</param>
        /// <returns>Item set if file was found or null if not</returns>
        public static ItemSet readInItemSet(string championKey = "", ItemSet.Map map = ItemSet.Map.SummonersRift, string name = "", string subDir = "")
        {
            //Get the directory we are going to write out to
            string itemSetDir = (String.IsNullOrEmpty(subDir)) ? getItemSetDirectory(championKey) : Path.Combine(subDir, championKey == "" ? "Global" : championKey) + Path.DirectorySeparatorChar;

            //Create our unique file name for the system
            string fileName = Path.Combine(itemSetDir, getItemSetFileName(championKey, map, name));

            //Make sure we have this file
            if (!File.Exists(fileName))
                return null;

            //Read in the file
            string JSON = File.ReadAllText(fileName);

            //Convert the JSON string to an object
            ItemSet itemSet = JsonConvert.DeserializeObject<ItemSet>(JSON);
            return itemSet;
        }
    }
}
