using MovingCoop.Application;
namespace MovingCoop.UnityAdapters
{
    public static class SnapshotNotice
    {
        public static string Apply(int previousPhase, SessionFrame incoming, string current)
        {
            if (incoming.phase == 1 && previousPhase != 1) return "SHIFT ACTIVE";
            if (incoming.canStart) return incoming.players?.Length == 1 ? "Ready. Host can start solo or wait for a second mover." : "All movers ready. Host can start.";
            if (incoming.phase == 0) return "Waiting for all connected movers to be ready.";
            return current;
        }
    }
}
