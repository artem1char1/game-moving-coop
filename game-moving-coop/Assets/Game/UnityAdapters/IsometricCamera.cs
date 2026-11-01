using System.Collections.Generic;
using UnityEngine;

namespace MovingCoop.UnityAdapters
{
    public sealed class IsometricCamera : MonoBehaviour
    {
        private readonly List<Renderer> hidden = new();
        public Transform Target { get; set; }
        public readonly List<Vector3> CargoOcclusionPoints = new();
        private void LateUpdate() => UpdateView(Time.unscaledDeltaTime);
        public void UpdateView(float deltaTime)
        {
            foreach (var renderer in hidden) if (renderer != null) renderer.enabled = true;
            hidden.Clear();
            if (Target == null) return;
            transform.rotation = Quaternion.Euler(50, 45, 0);
            var goal = Target.position - transform.forward * 17;
            transform.position = Vector3.Lerp(transform.position, goal, 1 - Mathf.Exp(-8 * deltaTime));
            HideToward(Target.position);
            foreach(var point in CargoOcclusionPoints) HideToward(point);
        }
        private void HideToward(Vector3 point)
        {
            var direction = point - transform.position;
            foreach (var hit in Physics.SphereCastAll(transform.position, 0.4f, direction.normalized, direction.magnitude, 1 << PlayerWorld.ObstacleLayer))
            {
                if (!hit.collider.TryGetComponent<WallOccluder>(out _)) continue;
                var renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null) { renderer.enabled = false; hidden.Add(renderer); }
            }
        }
        private void OnDisable() { foreach (var renderer in hidden) if (renderer != null) renderer.enabled = true; hidden.Clear(); }
    }
}
