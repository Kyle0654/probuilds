
namespace ProBuilds.Pipeline
{
    public class TestSynchronizer
    {
        public long Limit = 10000; // Max matches to process (pulls from API if not enough on disk)
        public long Count = 0;
    }
}