using System;
using MovingCoop.Domain;

namespace MovingCoop.Application
{
    [Serializable]
    public sealed class PlayerFrame { public ulong id; public float x, z; public bool ready; public int targetCargo, targetHandle; public bool rotationLeader; }
    [Serializable]
    public sealed class SessionFrame
    {
        public int protocol = 1;
        public string session;
        public int shift, revision, phase;
        public bool canStart;
        public PlayerFrame[] players;
        public CargoFrame[] cargo; public int total; public float remaining; public string result, feedback;
    }
    [Serializable]
    public sealed class ClientCommand
    {
        public int protocol = 1;
        public string session;
        public int shift, sequence;
        public string kind;
        public float x, z, turn;
    }
    [Serializable]
    public sealed class CargoFrame { public int id; public float x,z,angle; public bool delivered; public long first=-1,second=-1; }
    public interface ICargoWorld : IPlayerWorld { bool Clear(GroundPoint from, GroundPoint to); bool CargoFits(CargoState cargo, GroundPoint at, float angle); }
    public interface IPlayerWorld
    {
        GroundPoint Move(GroundPoint origin, GroundPoint displacement);
    }
}
