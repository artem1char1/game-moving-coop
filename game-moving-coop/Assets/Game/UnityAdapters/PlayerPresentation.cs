using System.Collections.Generic;
using MovingCoop.Application;
using UnityEngine;

namespace MovingCoop.UnityAdapters
{
    public sealed class PlayerPresentation
    {
        private readonly Dictionary<ulong, GameObject> views = new();
        private readonly Dictionary<ulong, Vector3> targets = new();
        private readonly Transform parent;
        public PlayerPresentation(Transform parent) => this.parent = parent;
        public Transform LocalTarget(ulong id) => views.TryGetValue(id, out var view) ? view.transform : null;
        public void Apply(SessionFrame frame)
        {
            var present = new HashSet<ulong>();
            foreach (var p in frame.players)
            {
                present.Add(p.id);
                if (!views.ContainsKey(p.id))
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.name = "Mover_" + p.id;
                    go.transform.SetParent(parent);
                    go.transform.localScale = new Vector3(0.5f, 0.75f, 0.5f);
                    Object.Destroy(go.GetComponent<Collider>());
                    var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.color = p.id == 0 ? new Color(0.18f, 0.6f, 0.95f) : new Color(1, 0.55f, 0.15f);
                    go.GetComponent<Renderer>().material = material;
                    go.transform.position = new Vector3(p.x, 0.75f, p.z);
                    views.Add(p.id, go);
                }
                targets[p.id] = new Vector3(p.x, 0.75f, p.z);
            }
            var remove = new List<ulong>();
            foreach (var pair in views) if (!present.Contains(pair.Key)) remove.Add(pair.Key);
            foreach (var id in remove) { DestroyView(id); }
        }
        public void Update(float dt, bool authority)
        {
            foreach (var pair in views)
                pair.Value.transform.position = authority ? targets[pair.Key] : Vector3.Lerp(pair.Value.transform.position, targets[pair.Key], 1 - Mathf.Exp(-18 * dt));
        }
        private void DestroyView(ulong id)
        {
            Object.Destroy(views[id].GetComponent<Renderer>().material);
            Object.Destroy(views[id]); views.Remove(id); targets.Remove(id);
        }
        public void Clear() { foreach (var id in new List<ulong>(views.Keys)) DestroyView(id); }
    }
}
