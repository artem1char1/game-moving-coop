using System.Collections.Generic;
using UnityEngine;

namespace MovingCoop
{
    // Every passage in the navigation graph is also an opening in the physical map.
    internal sealed class MazeGenerator : MonoBehaviour, ICourseMap
    {
        private Transform firstWall;
        private Transform hurdle;
        private Transform firstPillar;
        private Transform player;

        private const int Columns = 5;
        private const int Rows = 16;
        private const int CellCount = Columns * Rows;
        private const int Entrance = 2;
        private const int Exit = CellCount - 3;
        private const float CellSize = 4f;
        private const float WallThickness = .35f;
        private const float WallHeight = 2.4f;
        private const float GateHeight = 1.9f;

        private readonly bool[,] passages = new bool[CellCount, CellCount];
        private readonly Dictionary<int, Transform> jumps = new();
        private readonly Dictionary<int, Transform> gatesByEdge = new();
        private readonly List<(Transform Gate, float Down, float Up, float Travel, float Offset)> gates = new();
        private CourseBot[] bots;
        private Material wallMaterial;
        private Material jumpMaterial;
        private Material gateMaterial;
        private Transform generated;
        private int protectedDeadEnd;
        private int firstCrossing;
        private readonly List<(int A, int B)> shortcuts = new();

        internal void Initialize(Transform wallTemplate, Transform jumpTemplate, Transform gateTemplate, Transform racer)
        {
            firstWall = wallTemplate;
            hurdle = jumpTemplate;
            firstPillar = gateTemplate;
            player = racer;
            wallMaterial = firstWall.GetComponent<Renderer>().sharedMaterial;
            jumpMaterial = hurdle.GetComponent<Renderer>().sharedMaterial;
            gateMaterial = firstPillar.GetComponent<Renderer>().sharedMaterial;

            generated = new GameObject("Generated Labyrinth").transform;
            generated.SetParent(transform, false);
            firstCrossing = Random.value < .5f ? 0 : Columns - 1;
            var corners = new[] { 0, Columns - 1, CellCount - Columns, CellCount - 1 };
            protectedDeadEnd = corners[Random.Range(0, corners.Length)];
            CarveMap();
            for (var row = 3; row <= 11; row += 4)
            {
                var normalColumn = row == 7 ? Columns - 1 - firstCrossing : firstCrossing;
                var column = Columns - 1 - normalColumn;
                var a = row * Columns + column;
                var b = a + Columns;
                passages[a, b] = passages[b, a] = true;
                shortcuts.Add((a, b));
            }
            BuildWalls();
            PopulatePassages();
        }

        private static Vector2 Center(int cell) =>
            new((cell % Columns - 2) * CellSize, (cell / Columns - 7.5f) * CellSize);

        private static int Edge(int a, int b) => Mathf.Min(a, b) * CellCount + Mathf.Max(a, b);

        private IEnumerable<int> Neighbors(int cell)
        {
            var x = cell % Columns;
            var z = cell / Columns;
            if (x > 0 && CanLink(cell, cell - 1)) yield return cell - 1;
            if (x < Columns - 1 && CanLink(cell, cell + 1)) yield return cell + 1;
            if (z > 0 && CanCross(x, z - 1) && CanLink(cell, cell - Columns)) yield return cell - Columns;
            if (z < Rows - 1 && CanCross(x, z) && CanLink(cell, cell + Columns)) yield return cell + Columns;
        }

        private bool CanLink(int a, int b)
        {
            if (a != protectedDeadEnd && b != protectedDeadEnd) return true;
            var entrance = protectedDeadEnd % Columns == 0 ? protectedDeadEnd + 1 : protectedDeadEnd - 1;
            return a == entrance || b == entrance;
        }

        private bool CanCross(int x, int lowerRow)
        {
            if (lowerRow != 3 && lowerRow != 7 && lowerRow != 11) return true;
            var opening = lowerRow == 7 ? Columns - 1 - firstCrossing : firstCrossing;
            return x == opening;
        }

        private void CarveMap()
        {
            var visited = new bool[CellCount];
            var stack = new Stack<int>();
            stack.Push(Entrance);
            visited[Entrance] = true;
            while (stack.Count > 0)
            {
                var cell = stack.Peek();
                var choices = new List<int>();
                foreach (var next in Neighbors(cell))
                    if (!visited[next]) choices.Add(next);
                if (choices.Count == 0) { stack.Pop(); continue; }
                var chosen = choices[Random.Range(0, choices.Count)];
                passages[cell, chosen] = passages[chosen, cell] = true;
                visited[chosen] = true;
                stack.Push(chosen);
            }

            var extras = new List<(int A, int B)>();
            for (var cell = 0; cell < CellCount; cell++)
                foreach (var next in Neighbors(cell))
                    if (next > cell && !passages[cell, next] &&
                        cell != protectedDeadEnd && next != protectedDeadEnd)
                        extras.Add((cell, next));
            Shuffle(extras);
            var opened = 0;
            foreach (var edge in extras)
            {
                passages[edge.A, edge.B] = passages[edge.B, edge.A] = true;
                opened++;
                if (opened >= 9 && CountPaths(Entrance, new bool[CellCount], 3) >= 3) break;
            }
        }

        private int CountPaths(int cell, bool[] visited, int limit)
        {
            if (cell == Exit) return 1;
            visited[cell] = true;
            var count = 0;
            foreach (var next in Neighbors(cell))
                if (passages[cell, next] && !visited[next])
                {
                    count += CountPaths(next, visited, limit - count);
                    if (count >= limit) break;
                }
            visited[cell] = false;
            return count;
        }

        private static void Shuffle<T>(List<T> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var pick = Random.Range(0, i + 1);
                (items[i], items[pick]) = (items[pick], items[i]);
            }
        }

        private Transform Block(string name, Vector2 position, float width, float height, float depth, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(generated, false);
            block.transform.localPosition = new Vector3(position.x, height * .5f, position.y);
            block.transform.localScale = new Vector3(width, height, depth);
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block.transform;
        }

        private void BuildWalls()
        {
            // Seal the strip between the maze and the arena boundary.
            Block("Outer boundary", new Vector2(-10.6f, 0f), 1.2f, WallHeight, 72.4f, wallMaterial);
            Block("Outer boundary", new Vector2(10.6f, 0f), 1.2f, WallHeight, 72.4f, wallMaterial);
            // Merge neighboring wall edges into continuous longitudinal runs.
            for (var boundary = 0; boundary <= Columns; boundary++)
            {
                var runStart = -1;
                for (var z = 0; z <= Rows; z++)
                {
                    var solid = z < Rows && (boundary == 0 || boundary == Columns ||
                        !passages[z * Columns + boundary - 1, z * Columns + boundary]);
                    if (solid && runStart < 0) runStart = z;
                    if (!solid && runStart >= 0)
                    {
                        Block("Longitudinal Wall", new Vector2(-10f + boundary * CellSize,
                            -32f + (runStart + z) * CellSize * .5f),
                            WallThickness, WallHeight, (z - runStart) * CellSize + WallThickness, wallMaterial);
                        runStart = -1;
                    }
                }
            }

            for (var boundary = 0; boundary <= Rows; boundary++)
            {
                var runStart = -1;
                for (var x = 0; x <= Columns; x++)
                {
                    var solid = x < Columns && (boundary == 0 || boundary == Rows
                        ? x != 2
                        : !passages[(boundary - 1) * Columns + x, boundary * Columns + x]);
                    if (solid && runStart < 0) runStart = x;
                    if (!solid && runStart >= 0)
                    {
                        Block("Cross Wall", new Vector2(-10f + (runStart + x) * CellSize * .5f,
                            -32f + boundary * CellSize),
                            (x - runStart) * CellSize + WallThickness, WallHeight, WallThickness, wallMaterial);
                        runStart = -1;
                    }
                }
            }
        }

        private void PopulatePassages()
        {
            var edges = new List<(int A, int B)>();
            for (var cell = 0; cell < CellCount; cell++)
                foreach (var next in Neighbors(cell))
                    if (next > cell && passages[cell, next] &&
                        cell != protectedDeadEnd && next != protectedDeadEnd)
                        edges.Add((cell, next));
            Shuffle(edges);
            for (var i = 0; i < Mathf.Min(21, edges.Count); i++)
            {
                var edge = edges[i];
                var horizontalTravel = edge.B - edge.A == 1;
                var center = (Center(edge.A) + Center(edge.B)) * .5f;
                var lift = i < 3;
                var thickness = lift ? .65f : .3f;
                var obstacle = Block(lift ? "Corridor Lift Gate" : "Corridor Jump Barrier", center,
                    horizontalTravel ? thickness : CellSize, lift ? GateHeight : Random.Range(.42f, .55f),
                    horizontalTravel ? CellSize : thickness, lift ? gateMaterial : jumpMaterial);
                var key = Edge(edge.A, edge.B);
                if (lift)
                {
                    gatesByEdge.Add(key, obstacle);
                    gates.Add((obstacle, Random.Range(2f, 2.8f), Random.Range(1f, 2f),
                        Random.Range(.5f, .8f), Random.Range(0f, 3f)));
                    var position = obstacle.localPosition;
                    position.y = -GateHeight * .5f;
                    obstacle.localPosition = position;
                }
                else jumps.Add(key, obstacle);
            }
        }

        internal bool IsPassage(int a, int b) => passages[a, b];

        internal bool IsDeadEnd(int cell)
        {
            var degree = 0;
            for (var next = 0; next < CellCount; next++)
                if (passages[cell, next]) degree++;
            return degree == 1 && cell != Entrance && cell != Exit;
        }

        public void AddFeatures(CourseFeatures features)
        {
            foreach (var edge in shortcuts)
            {
                var middle = (Center(edge.A) + Center(edge.B)) * .5f;
                features.AddRiskyPassage(new Rect(middle.x - 1.82f, middle.y - .9f, 3.64f, 1.8f), .65f);
                var exit = Center(edge.B);
                features.AddCheckpoint(new Vector3(exit.x, .08f, exit.y));
            }
            for (var row = 3; row <= 11; row += 4)
            {
                var column = row == 7 ? Columns - 1 - firstCrossing : firstCrossing;
                var center = Center((row + 1) * Columns + column);
                features.AddCheckpoint(new Vector3(center.x, .08f, center.y));
            }
            var minimap = gameObject.AddComponent<MazeMinimap>();
            minimap.Initialize(this, player);
        }

        public void TrackBots(CourseBot[] racers) => bots = racers;

        public CourseBot.Waypoint[] BotRoute(float spawnX, float lane, float finishZ)
        {
            var parent = new int[CellCount];
            System.Array.Fill(parent, -1);
            var queue = new Queue<int>();
            queue.Enqueue(Entrance);
            parent[Entrance] = Entrance;
            while (queue.Count > 0 && parent[Exit] < 0)
            {
                var cell = queue.Dequeue();
                var choices = new List<int>(Neighbors(cell));
                if (lane > .5f) choices.Reverse();
                foreach (var next in choices)
                    if (passages[cell, next] && parent[next] < 0)
                    {
                        parent[next] = cell;
                        queue.Enqueue(next);
                    }
            }
            var path = new List<int>();
            for (var cell = Exit; cell != Entrance; cell = parent[cell]) path.Add(cell);
            path.Add(Entrance);
            path.Reverse();

            var route = new List<CourseBot.Waypoint>();
            var entry = Center(Entrance);
            route.Add(new CourseBot.Waypoint(entry.x, -33f));
            route.Add(new CourseBot.Waypoint(entry.x, entry.y));
            for (var i = 1; i < path.Count; i++)
            {
                var from = Center(path[i - 1]);
                var to = Center(path[i]);
                var key = Edge(path[i - 1], path[i]);
                var middle = (from + to) * .5f;
                var direction = (to - from).normalized;
                if (jumps.ContainsKey(key) || gatesByEdge.ContainsKey(key))
                {
                    var before = middle - direction * 1.05f;
                    var after = middle + direction * 1.05f;
                    gatesByEdge.TryGetValue(key, out var gate);
                    route.Add(new CourseBot.Waypoint(before.x, before.y));
                    route.Add(new CourseBot.Waypoint(after.x, after.y, jumps.ContainsKey(key), gate));
                }
                route.Add(new CourseBot.Waypoint(to.x, to.y));
            }
            route.Add(new CourseBot.Waypoint(0f, finishZ + .7f));
            return route.ToArray();
        }

        private void Update() => UpdateGates(Time.timeSinceLevelLoad);

        private void UpdateGates(float time)
        {
            foreach (var gate in gates)
            {
                var phase = Mathf.Repeat(time + gate.Offset, gate.Down + gate.Up + gate.Travel * 2f);
                var height = phase < gate.Down ? 0f :
                    phase < gate.Down + gate.Travel ? Mathf.SmoothStep(0f, GateHeight, (phase - gate.Down) / gate.Travel) :
                    phase < gate.Down + gate.Travel + gate.Up ? GateHeight :
                    Mathf.SmoothStep(GateHeight, 0f, (phase - gate.Down - gate.Travel - gate.Up) / gate.Travel);
                var position = gate.Gate.localPosition;
                var y = height - GateHeight * .5f;
                if (y > position.y && Occupied(gate.Gate)) continue;
                position.y = y;
                gate.Gate.localPosition = position;
            }
        }

        private bool Occupied(Transform gate)
        {
            var halfSize = gate.localScale * .5f + new Vector3(.32f, 0f, .32f);
            bool Contains(Vector3 position) => Mathf.Abs(position.x - gate.position.x) < halfSize.x &&
                Mathf.Abs(position.z - gate.position.z) < halfSize.z;
            if (Contains(player.position)) return true;
            if (bots != null)
                foreach (var bot in bots)
                    if (Contains(bot.Position)) return true;
            return false;
        }
    }
}
