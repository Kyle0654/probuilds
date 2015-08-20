using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public class RiotVersion
    {
        /// <summary>
        /// Tolerance to use when comparing versions
        /// </summary>
        public enum MatchTolerance
        {
            Major,
            Minor,
            Patch,
            SubPatch
        }

        public string Major;
        public string Minor;
        public string Patch;
        public string SubPatch;

        private string version;

        public RiotVersion(string version)
        {
            this.version = version;

            if (string.IsNullOrWhiteSpace(version))
            {
                Major = Minor = Patch = SubPatch = null;
                return;
            }

            string[] splits = version.Split('.');
            Major = splits.Length > 0 ? splits[0] : null;
            Minor = splits.Length > 1 ? splits[1] : null;
            Patch = splits.Length > 2 ? splits[2] : null;
            SubPatch = splits.Length > 3 ? splits[3] : null;
        }

        /// <summary>
        /// Test whether this version matches another version, to a specified tolerance.
        /// </summary>
        public bool IsSamePatch(RiotVersion other, MatchTolerance tolerance = MatchTolerance.Minor)
        {
            if (Major != other.Major) return false;
            if (tolerance == MatchTolerance.Major) return true;
            if (Minor != other.Minor) return false;
            if (tolerance == MatchTolerance.Minor) return true;
            if (Patch != other.Patch) return false;
            if (tolerance == MatchTolerance.Patch) return false;
            return (SubPatch != other.SubPatch);
        }

        public override string ToString()
        {
            return version;
        }
    }
}
