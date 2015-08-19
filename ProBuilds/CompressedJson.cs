using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
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

        private static void EnsureDirectory(string path)
        {
            string dirPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
        }
    }
}
