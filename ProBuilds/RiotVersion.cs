using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds
{
    public class RiotVersion : IComparable
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

        public int CompareTo(object obj)
        {
            if (!(obj is RiotVersion))
                return -1;

            if (ReferenceEquals(this, obj))
                return 0;

            RiotVersion other = obj as RiotVersion;
            int diff = Major.CompareTo(other.Major);
            if (diff != 0 || Minor == null) return diff;

            diff = Minor.CompareTo(other.Minor);
            if (diff != 0 || Patch == null) return diff;

            diff = Patch.CompareTo(other.Patch);
            if (diff != 0 || SubPatch == null) return diff;

            diff = SubPatch.CompareTo(other.SubPatch);
            return diff;
        }

        public static bool operator <(RiotVersion a, RiotVersion b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(RiotVersion a, RiotVersion b)
        {
            return a.CompareTo(b) > 0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RiotVersion))
                return false;

            if (ReferenceEquals(this, obj))
                return true;


            RiotVersion other = obj as RiotVersion;
            return version == other.version;
        }

        public override int GetHashCode()
        {
            return version.GetHashCode();
        }
    }
}
