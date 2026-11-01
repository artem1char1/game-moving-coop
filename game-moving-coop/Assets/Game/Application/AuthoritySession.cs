using System;
using System.Collections.Generic;
using System.Linq;
using MovingCoop.Domain;

namespace MovingCoop.Application
{
    public sealed class AuthoritySession
    {
        private sealed class InputState
        {
            public int Sequence = -1;
            public float X, Z, Turn;
            public double ReceivedAt;
        }
        private readonly Queue<(ulong sender, ClientCommand command)> inbox = new();
        private readonly Dictionary<ulong, InputState> inputs = new();
        private readonly Dictionary<ulong, int> operations = new();
        private readonly IPlayerWorld world;
        private readonly CargoSimulation cargo; private double deadline, clock; private string result="";
        public SessionState State { get; } = new();
        public event Action<string, ulong> Decision;
        public AuthoritySession(IPlayerWorld world) { this.world=world; cargo=new CargoSimulation((ICargoWorld)world); }
        public void Connect(ulong id, bool host)
        {
            if (!State.Add(id)) return;
            inputs[id] = new InputState();
            if (host) State.MarkReady(id);
        }
        public void Disconnect(ulong id)
        {
            cargo.Cancel(id); State.Remove(id); inputs.Remove(id); operations.Remove(id);
            Decision?.Invoke("disconnect", id);
        }
        public void Enqueue(ulong sender, ClientCommand command)
        {
            if (command != null && inbox.Count < 256) inbox.Enqueue((sender, command));
        }
        public void Tick(double now, float dt)
        {
            clock=now;
            if(State.Phase==ShiftPhase.Active && now>=deadline) End("Deadline");
            while (inbox.Count > 0)
            {
                var entry = inbox.Dequeue();
                Handle(entry.sender, entry.command, now);
            }
            if (State.Phase != ShiftPhase.Active) return;
            foreach (var p in State.Players)
            {
                var input = inputs[p.Id];
                if (now - input.ReceivedAt >= 0.5) { input.X = 0; input.Z = 0; input.Turn=0; cargo.Cancel(p.Id); }
                if(cargo.Held(p.Id)!=null || cargo.Approaching(p.Id)) continue;
                if (input.X == 0 && input.Z == 0) continue;
                State.Face(p.Id,Heading.Towards(p.Facing,Heading.FromInput(input.X,input.Z),540*dt));
                State.Move(p.Id, world.Move(p.Position, new GroundPoint(input.X * 4 * dt, input.Z * 4 * dt)));
            }
            cargo.Step(State,dt,now,id=> {var v=inputs[id];return (v.X,v.Z,v.Turn);});
        }
        private void End(string reason) { State.Finish(); result=(cargo.Total>=320?"Success: ":"Failure: ")+reason; foreach(var p in State.Players)cargo.Cancel(p.Id); }
        private void Handle(ulong sender, ClientCommand c, double now)
        {
            if (c.protocol != 1 || c.session != State.SessionId || c.shift != State.ShiftId || !inputs.TryGetValue(sender, out var input)) return;
            if (c.kind == "input")
            {
                if (c.sequence <= input.Sequence || !Finite(c.x) || !Finite(c.z)) return;
                input.Sequence = c.sequence;
                var length = Math.Sqrt(c.x * c.x + c.z * c.z);
                if (length > 1.001) return;
                input.Turn=Finite(c.turn)?Math.Max(-1,Math.Min(1,c.turn)):0;
                input.X = c.x; input.Z = c.z; input.ReceivedAt = now;
                return;
            }
            if (operations.TryGetValue(sender, out var previous) && c.sequence <= previous) return;
            operations[sender] = c.sequence;
            switch (c.kind)
            {
                case "ready": if (State.MarkReady(sender)) Decision?.Invoke("ready", sender); break;
                case "start":
                    if (sender == 0 && State.Start()) { deadline=now+300; Decision?.Invoke("started", sender); }
                    else Decision?.Invoke("start_rejected", sender);
                    break;
                case "interact": if(State.Phase==ShiftPhase.Active)cargo.Interact(State,sender); break;
                case "evacuate": if(sender==0 && State.Phase==ShiftPhase.Active && State.Players.All(p=>p.Position.X>=-2&&p.Position.X<=2&&p.Position.Z>=-7.5f&&p.Position.Z<=-4.5f&&cargo.Held(p.Id)==null&&!cargo.Approaching(p.Id))) End("Evacuated"); break;
                case "restart": if(sender==0 && State.Phase==ShiftPhase.Finished){State.Restart();cargo.Reset();operations.Clear();result="";foreach(var v in inputs.Values){v.X=v.Z=v.Turn=0;v.Sequence=-1;}} break;
                case "focus": cargo.Cancel(sender); input.Turn=0; input.X = 0; input.Z = 0; Decision?.Invoke("focus_release", sender); break;
            }
        }
        private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        public SessionFrame Snapshot() => new()
        {
            session = State.SessionId, shift = State.ShiftId, revision = State.Revision,
            phase = (int)State.Phase, canStart = State.CanStart, total=cargo.Total, remaining=State.Phase==ShiftPhase.Preparing?300:(float)Math.Max(0,deadline-clock),result=result,feedback=cargo.Feedback,
            cargo=cargo.Items.Select(c=>new CargoFrame{id=c.Id,x=c.Position.X,z=c.Position.Z,angle=c.Angle,delivered=c.Delivered,first=c.Carriers.TryGetValue(0,out var a)?(long)a:-1,second=c.Carriers.TryGetValue(1,out var b)?(long)b:-1}).ToArray(),
            players = State.Players.OrderBy(p => p.Id).Select(p => new PlayerFrame { id = p.Id, x = p.Position.X, z = p.Position.Z, facing = p.Facing, ready = p.Ready,targetCargo=cargo.Target(State,p.Id).cargo,targetHandle=cargo.Target(State,p.Id).handle,rotationLeader=cargo.Held(p.Id)?.Leader==p.Id }).ToArray()
        };
    }
}
