using MovingCoop.Application;
using MovingCoop.Domain;
using UnityEngine;

namespace MovingCoop.UnityAdapters
{
    public sealed class PlayerWorld : ICargoWorld
    {
        public bool Clear(GroundPoint from,GroundPoint to) { var moved=Move(from,new GroundPoint(to.X-from.X,to.Z-from.Z)); return Mathf.Abs(moved.X-to.X)<.005f && Mathf.Abs(moved.Z-to.Z)<.005f && !Physics.CheckCapsule(new Vector3(to.X,Radius,to.Z),new Vector3(to.X,Height-Radius,to.Z),Radius,1<<ObstacleLayer,QueryTriggerInteraction.Ignore); }
        public bool CargoFits(CargoState cargo,GroundPoint at,float angle) {
            var half=new Vector3(cargo.Sofa?1.1f:.3f,cargo.Sofa?.45f:.3f,cargo.Sofa?.45f:.3f);
            // Sample translation and swept rotation in small increments; reject entire unsafe group step.
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(angle-cargo.Angle)/1f));
            for(int i=0;i<=steps;i++){float t=(float)i/steps;var p=Vector3.Lerp(new Vector3(cargo.Position.X,half.y,cargo.Position.Z),new Vector3(at.X,half.y,at.Z),t);if(Physics.CheckBox(p,half,Quaternion.Euler(0,Mathf.Lerp(cargo.Angle,angle,t),0),1<<ObstacleLayer,QueryTriggerInteraction.Ignore))return false;}return true;
        }
        public const int ObstacleLayer = 8;
        public const float Radius = 0.25f, Height = 1.5f, Skin = 0.01f;
        public GroundPoint Move(GroundPoint origin, GroundPoint displacement)
        {
            var position = new Vector3(origin.X, 0, origin.Z);
            var remaining = new Vector3(displacement.X, 0, displacement.Z);
            for (var i = 0; i < 3 && remaining.sqrMagnitude > 0.000001f; i++)
            {
                var distance = remaining.magnitude;
                if (!Physics.CapsuleCast(position + Vector3.up * Radius, position + Vector3.up * (Height - Radius), Radius,
                        remaining / distance, out var hit, distance + Skin, 1 << ObstacleLayer, QueryTriggerInteraction.Ignore))
                { position += remaining; break; }
                var advance = Mathf.Clamp(hit.distance - Skin, 0, distance);
                position += remaining.normalized * advance;
                remaining = Vector3.ProjectOnPlane(remaining.normalized * (distance - advance), hit.normal);
                remaining.y = 0;
            }
            // Yard safety limits match the authored floor; movement never walks into empty space.
            return new GroundPoint(Mathf.Clamp(position.x, -10.5f, 10.5f), Mathf.Clamp(position.z, -9.5f, 8.5f));
        }
    }
}
