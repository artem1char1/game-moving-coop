using UnityEngine;

namespace MovingCoop
{
    internal sealed class MazeMinimap : MonoBehaviour
    {
        private MazeGenerator maze;
        private Transform player;
        private readonly bool[] visited = new bool[80];
        private int current = -1;

        internal void Initialize(MazeGenerator generator, Transform racer)
        {
            maze = generator;
            player = racer;
        }

        private void Update()
        {
            var p = player.position;
            var x = Mathf.FloorToInt((p.x + 10f) / 4f);
            var z = Mathf.FloorToInt((p.z + 32f) / 4f);
            current = x >= 0 && x < 5 && z >= 0 && z < 16 ? z * 5 + x : -1;
            if (current >= 0 && p.y >= -.1f) visited[current] = true;
        }

        private void OnGUI()
        {
            const float cell = 10f;
            const float left = 22f;
            const float top = 116f;
            GUI.Box(new Rect(12f, 86f, 120f, 210f), "Исследовано");
            var old = GUI.color;
            for (var i = 0; i < visited.Length; i++)
            {
                var x = left + i % 5 * cell;
                var y = top + (15 - i / 5) * cell;
                GUI.color = visited[i] ? new Color(.6f, .63f, .68f) : new Color(.14f, .16f, .2f);
                GUI.DrawTexture(new Rect(x, y, 9f, 9f), Texture2D.whiteTexture);
                if (!visited[i]) continue;
                GUI.color = Color.white;
                if (i % 5 < 4 && visited[i + 1] && maze.IsPassage(i, i + 1))
                    GUI.DrawTexture(new Rect(x + 8f, y + 3f, 4f, 3f), Texture2D.whiteTexture);
                if (i < 75 && visited[i + 5] && maze.IsPassage(i, i + 5))
                    GUI.DrawTexture(new Rect(x + 3f, y - 2f, 3f, 4f), Texture2D.whiteTexture);
                if (maze.IsDeadEnd(i))
                {
                    GUI.color = new Color(1f, .4f, .4f);
                    GUI.DrawTexture(new Rect(x + 3f, y + 3f, 3f, 3f), Texture2D.whiteTexture);
                }
            }
            if (current >= 0)
            {
                GUI.color = Color.cyan;
                GUI.DrawTexture(new Rect(left + current % 5 * cell + 2f, top + (15 - current / 5) * cell + 2f, 5f, 5f),
                    Texture2D.whiteTexture);
            }
            GUI.color = old;
            GUI.Label(new Rect(20f, 278f, 110f, 18f), "Красный — тупик");
        }
    }
}
