
namespace ProBuilds.Pipeline
{
    public class MatchDownloadLimiter
    {
        public long Limit = 40000; // Max matches to process (pulls from API if not enough on disk)
        public long Count = 0;
    }
}