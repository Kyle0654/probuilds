using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds.IO
{
    static class CompressedJson
    {
        /// <summary>
        /// Saves an object to a compressed JSON file.
        /// </summary>
        public static void WriteToFile<T>(string filename, T obj)
        {
            EnsureDirectory(filename);

            using (FileStream file = File.Create(filename))
            {
                using (GZipStream stream = new GZipStream(file, CompressionLevel.Optimal))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                        writer.Write(json);
                    }
                }
            }
        }

        /// <summary>
        /// Loads an object from a compressed json file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static T ReadFromFile<T>(string filename)
        {
            using (FileStream file = File.OpenRead(filename))
            {
                using (GZipStream stream = new GZipStream(file, CompressionMode.Decompress))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        T obj = JsonConvert.DeserializeObject<T>(json);
                        return obj;
                    }
                }
            }
        }

        private static void EnsureDirectory(string path)
        {
            string dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
        }
    }
}
