using System.Collections.Generic;
using MovingCoop.Application;
using UnityEngine;

namespace MovingCoop.UnityAdapters
{
    public sealed class PlayerPresentation
    {
        private readonly Dictionary<ulong, GameObject> views = new();
        private readonly Dictionary<ulong, Vector3> targets = new();
        private readonly Dictionary<ulong,float> headings = new();
        private readonly Dictionary<ulong,Transform> models = new();
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
                    var go = new GameObject();
                    go.name = "Mover_" + p.id;
                    go.transform.SetParent(parent);

                    var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.color = p.id == 0 ? new Color(0.18f, 0.6f, 0.95f) : new Color(1, 0.55f, 0.15f);

                    go.transform.position = new Vector3(p.x, 0.75f, p.z);
                    // Replace this visual child with the future humanoid prefab; root/network pose stays stable.
                    var visual = new GameObject("ModelRoot").transform;visual.SetParent(go.transform,false);
                    var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="PlaceholderBody";body.transform.SetParent(visual,false);body.transform.localScale=new Vector3(.5f,.75f,.5f);Object.Destroy(body.GetComponent<Collider>());body.GetComponent<Renderer>().material=material;
                    var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);nose.name="ForwardMarker";nose.transform.SetParent(visual,false);nose.transform.localPosition=new Vector3(0,.35f,.38f);nose.transform.localScale=new Vector3(.35f,.2f,.4f);Object.Destroy(nose.GetComponent<Collider>());
                    models[p.id]=visual;
                    views.Add(p.id, go);
                }
                headings[p.id]=p.facing;
                targets[p.id] = new Vector3(p.x, 0.75f, p.z);
            }
            var remove = new List<ulong>();
            foreach (var pair in views) if (!present.Contains(pair.Key)) remove.Add(pair.Key);
            foreach (var id in remove) { DestroyView(id); }
        }
        public void Update(float dt, bool authority)
        {
            foreach (var pair in views) {
                pair.Value.transform.position = authority ? targets[pair.Key] : Vector3.Lerp(pair.Value.transform.position, targets[pair.Key], 1 - Mathf.Exp(-18 * dt));
                models[pair.Key].rotation=Quaternion.RotateTowards(models[pair.Key].rotation,Quaternion.Euler(0,headings[pair.Key],0),540*dt);
            }
        }
        private void DestroyView(ulong id)
        {
            foreach(var renderer in views[id].GetComponentsInChildren<Renderer>()) Object.Destroy(renderer.material);
            Object.Destroy(views[id]); views.Remove(id); targets.Remove(id);headings.Remove(id);models.Remove(id);
        }
        public void Clear() { foreach (var id in new List<ulong>(views.Keys)) DestroyView(id); }
    }
}
