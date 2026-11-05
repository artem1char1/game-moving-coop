using System.Collections.Generic;
using UnityEngine;

namespace MovingCoop
{
    internal sealed class CourseFeatures : MonoBehaviour
    {
        private readonly List<Rect> pits = new();
        private readonly List<Vector3> checkpoints = new();
        private PlayerMovement player;
        private Vector3 respawn;
        private int checkpointIndex = -1;
        private Material groundMaterial;
        private Material markerMaterial;
        private string notice = "";
        private float noticeUntil;

        internal void Initialize(PlayerMovement racer, Material markers)
        {
            player = racer;
            respawn = player.transform.position;
            markerMaterial = markers;
            groundMaterial = GameObject.Find("Flat Arena").GetComponent<Renderer>().sharedMaterial;
        }

        internal void AddCheckpoint(Vector3 position)
        {
            position.y = .08f;
            checkpoints.Add(position);
            Surface("Checkpoint", new Rect(position.x - .65f, position.z - .3f, 1.3f, .6f), .018f, markerMaterial);
        }

        internal void AddRiskyPassage(Rect pit, float bridgeWidth)
        {
            pits.Add(pit);
            Surface("Narrow shortcut bridge", new Rect(pit.center.x - bridgeWidth * .5f, pit.yMin,
                bridgeWidth, pit.height), .04f, markerMaterial);
            var sign = new GameObject("Shortcut sign").AddComponent<TextMesh>();
            sign.transform.SetParent(transform, false);
            sign.transform.localPosition = new Vector3(pit.center.x, .1f, pit.yMin - .35f);
            sign.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            sign.text = "Срезка";
            sign.anchor = TextAnchor.MiddleCenter;
            sign.characterSize = .18f;
            sign.fontSize = 48;
            sign.color = new Color(1f, .8f, .15f);
        }

        private void Surface(string name, Rect area, float top, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(transform, false);
            block.transform.localPosition = new Vector3(area.center.x, top - .25f, area.center.y);
            block.transform.localScale = new Vector3(area.width, .5f, area.height);
            block.GetComponent<Renderer>().sharedMaterial = material;
        }

        internal void BuildTerrain()
        {
            var source = GameObject.Find("Flat Arena");
            source.GetComponent<Renderer>().enabled = false;
            source.GetComponent<Collider>().enabled = false;
            // Source dimensions stay available through its transform after the collider is disabled.
            var size = source.transform.localScale;
            var center = source.transform.position;
            var xs = new SortedSet<float> { center.x - size.x * .5f, center.x + size.x * .5f };
            var zs = new SortedSet<float> { center.z - size.z * .5f, center.z + size.z * .5f };
            foreach (var pit in pits)
            {
                xs.Add(pit.xMin); xs.Add(pit.xMax);
                zs.Add(pit.yMin); zs.Add(pit.yMax);
            }
            var xCuts = new List<float>(xs);
            var zCuts = new List<float>(zs);
            for (var x = 1; x < xCuts.Count; x++)
                for (var z = 1; z < zCuts.Count; z++)
                {
                    var area = Rect.MinMaxRect(xCuts[x - 1], zCuts[z - 1], xCuts[x], zCuts[z]);
                    if (!pits.Exists(pit => pit.Contains(area.center)))
                        Surface("Course ground", area, 0f, groundMaterial);
                }
        }

        private void Update()
        {
            var position = player.transform.position;
            if (position.y < -3f)
            {
                player.Respawn(respawn);
                notice = "Возврат к контрольной точке";
                noticeUntil = Time.unscaledTime + 2.5f;
                return;
            }
            if (position.y < -.1f || !player.GetComponent<CharacterController>().isGrounded) return;
            for (var i = 0; i < checkpoints.Count; i++)
            {
                var delta = position - checkpoints[i];
                delta.y = 0f;
                if (delta.sqrMagnitude > .8f * .8f || checkpointIndex == i) continue;
                checkpointIndex = i;
                respawn = checkpoints[i];
                notice = "Контрольная точка сохранена";
                noticeUntil = Time.unscaledTime + 2.5f;
                break;
            }
        }

        private void OnGUI()
        {
            if (Time.unscaledTime < noticeUntil)
                GUI.Box(new Rect((Screen.width - 320f) * .5f, 86f, 320f, 28f), notice);
        }
    }
}
