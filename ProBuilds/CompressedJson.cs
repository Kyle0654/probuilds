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
        public static void WriteToFile<T>(string filename, T obj)
        {
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
    }
}
