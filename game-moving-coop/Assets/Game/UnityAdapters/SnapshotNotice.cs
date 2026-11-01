using MovingCoop.Application;
namespace MovingCoop.UnityAdapters
{
    public static class SnapshotNotice
    {
        public static string Apply(int previousPhase, SessionFrame incoming, string current)
        {
            if (incoming.phase == 1 && previousPhase != 1) return "SHIFT ACTIVE";
            if (incoming.canStart) return "Both movers ready. Host can start.";
            return current;
        }
    }
}
