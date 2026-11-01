using System;
using System.Collections.Generic;
using System.Linq;

namespace MovingCoop.Domain
{
    public enum ShiftPhase { Preparing, Active, Finished }

    public readonly struct GroundPoint
    {
        public readonly float X, Z;
        public GroundPoint(float x, float z) { X = x; Z = z; }
    }

    public sealed class PlayerState
    {
        public ulong Id { get; }
        public GroundPoint Position { get; private set; }
        public bool Ready { get; private set; }
        public PlayerState(ulong id, GroundPoint position) { Id = id; Position = position; }
        internal void SetReady() => Ready = true;
        internal void MoveTo(GroundPoint position) => Position = position;
    }

    // No Unity or networking dependencies. Only the authority application mutates this aggregate.
    public sealed class SessionState
    {
        private readonly Dictionary<ulong, PlayerState> players = new();
        public string SessionId { get; } = Guid.NewGuid().ToString("N");
        public int ShiftId { get; private set; } = 1;
        public int Revision { get; private set; }
        public ShiftPhase Phase { get; private set; } = ShiftPhase.Preparing;
        public IEnumerable<PlayerState> Players => players.Values;
        public bool CanStart => Phase == ShiftPhase.Preparing && players.Count >= 1 && players.Values.All(p => p.Ready);
        public bool CanJoin => Phase == ShiftPhase.Preparing && players.Count < 2;

        public bool Add(ulong id)
        {
            if (!CanJoin || players.ContainsKey(id)) return false;
            players.Add(id, new PlayerState(id, new GroundPoint(players.Count == 0 ? -0.5f : 0.5f, -6)));
            Revision++;
            return true;
        }
        public void Remove(ulong id) { if (players.Remove(id)) Revision++; }
        public PlayerState Find(ulong id) => players.TryGetValue(id, out var p) ? p : null;
        public bool MarkReady(ulong id)
        {
            var p = Find(id);
            if (Phase != ShiftPhase.Preparing || p == null) return false;
            p.SetReady(); Revision++; return true;
        }
        public void Finish() { if (Phase == ShiftPhase.Active) { Phase = ShiftPhase.Finished; Revision++; } }
        public void Restart() { if (Phase != ShiftPhase.Finished) return; ShiftId++; Phase = ShiftPhase.Preparing; int i=0; foreach (var p in players.Values.OrderBy(p=>p.Id)) p.MoveTo(new GroundPoint(i++ == 0 ? -0.5f : 0.5f,-6)); Revision++; }
        public bool Start()
        {
            if (!CanStart) return false;
            Phase = ShiftPhase.Active; Revision++; return true;
        }
        public void Move(ulong id, GroundPoint point)
        {
            var p = Find(id);
            if (Phase != ShiftPhase.Active || p == null) return;
            p.MoveTo(point); Revision++;
        }
    }
}
