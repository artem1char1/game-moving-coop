using System.Collections.Generic;
using UnityEngine;

namespace MovingCoop
{
    internal sealed class ObstacleGenerator : MonoBehaviour, ICourseMap
    {
        private Transform hurdle;
        private Transform secondHurdle;
        private Transform centerBlock;
        private Transform firstWall;
        private Transform secondWall;
        private Transform sideHurdle;
        private Transform extraHurdle;
        private Transform extraWall;
        private Transform firstPillar;
        private Transform secondPillar;
        private Transform player;

        private const float HalfArenaWidth = 11f;
        private const float PillarHeight = 1.9f;
        private const float DividerThickness = .7f;
        private LiftCycle firstCycle;
        private LiftCycle secondCycle;
        private CourseBot[] bots;
        private Transform[] rows;
        private readonly Dictionary<Transform, WallCombination> combinations = new(4);
        private readonly Dictionary<Transform, float> dividerLengths = new(2);
        private readonly List<(Transform Gate, LiftCycle Cycle)> dividerGates = new(2);

        private readonly struct WallCombination
        {
            public readonly Transform Hurdle;
            public readonly Transform Gate;
            public readonly LiftCycle Cycle;
            public readonly float BypassX;

            public WallCombination(Transform hurdle, Transform gate, float bypassX)
            {
                Hurdle = hurdle;
                Gate = gate;
                BypassX = bypassX;
                Cycle = new LiftCycle(Random.Range(0f, 2.8f));
            }
        }

        private readonly HashSet<Transform> detours = new();
        internal void Initialize(Transform wallTemplate, Transform jumpTemplate, Transform gateTemplate, Transform racer)
        {
            player = racer;
            firstWall = Clone(wallTemplate, "Combined Wall A");
            secondWall = Clone(wallTemplate, "Combined Wall B");
            extraWall = Clone(wallTemplate, "Combined Wall C");
            hurdle = Clone(jumpTemplate, "Wide Hurdle A");
            secondHurdle = Clone(jumpTemplate, "Wide Hurdle B");
            sideHurdle = Clone(jumpTemplate, "Side Hurdle A");
            extraHurdle = Clone(jumpTemplate, "Side Hurdle B");
            centerBlock = Clone(wallTemplate, "Center Block");
            firstPillar = Clone(gateTemplate, "Full-width Gate A");
            secondPillar = Clone(gateTemplate, "Full-width Gate B");
            var thirdDivider = NewDivider("Longitudinal Wall C");
            var fourthDivider = NewDivider("Longitudinal Wall D");
            var detourA = Clone(wallTemplate, "Mandatory Detour A");
            var detourB = Clone(wallTemplate, "Mandatory Detour B");
            var detourC = Clone(wallTemplate, "Mandatory Detour C");
            detours.UnionWith(new[] { detourA, detourB, detourC });
            var fourthWall = Instantiate(extraWall, transform);
            fourthWall.name = "Fourth Combined Wall";
            var firstDivider = NewDivider("Longitudinal Wall A");
            var secondDivider = NewDivider("Longitudinal Wall B");
            rows = new[]
            {
                hurdle, secondHurdle, firstWall, secondWall, sideHurdle, centerBlock,
                firstPillar, secondPillar, extraHurdle, extraWall, fourthWall, firstDivider, secondDivider,
                thirdDivider, fourthDivider, detourA, detourB, detourC
            };
            Shuffle(rows);

            firstCycle = new LiftCycle(0f);
            secondCycle = new LiftCycle(Random.Range(1.2f, 2.8f));

            var dividerSpecs = new Dictionary<Transform, int>();
            var detourOnLeft = Random.value < .5f;
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                var z = -26f + i * (52f / (rows.Length - 1)) + Random.Range(-.06f, .06f);
                if (detours.Contains(row))
                {
                    Place(row, EdgeCenter(17.6f, detourOnLeft), z, 17.6f, 2.4f, .55f);
                    detourOnLeft = !detourOnLeft;
                }
                else if (row == hurdle || row == secondHurdle)
                    PlaceSideHurdle(row, z, Random.Range(13f, 16f));
                else if (row == firstPillar || row == secondPillar)
                    PlaceGate(row, z);
                else if (row == sideHurdle || row == extraHurdle)
                    PlaceSideHurdle(row, z, Random.Range(6f, 10f));
                else if (row == centerBlock)
                    Place(row, Random.Range(-2f, 2f), z, Random.Range(4f, 7f),
                        Random.Range(1.7f, 2.15f), Random.Range(.8f, 1.2f));
                else if (row == firstDivider || row == secondDivider || row == thirdDivider || row == fourthDivider)
                {
                    row.localPosition = new Vector3(0f, 0f, z);
                    dividerSpecs.Add(row, Random.Range(row == firstDivider ? 1 : 0, 4));
                }
                else
                    combinations.Add(row, CombineWall(row, z));
            }

            // Adjacent dividers connect, and the last one reaches the next obstacle.
            for (var i = rows.Length - 1; i >= 0; i--)
            {
                var row = rows[i];
                if (!dividerSpecs.TryGetValue(row, out var mix)) continue;
                var start = row.localPosition.z - .9f;
                var next = i + 1 < rows.Length ? rows[i + 1] : null;
                var end = 31.95f;
                var x = Random.Range(-5f, 5f);
                if (next != null)
                {
                    x = next.localPosition.x;
                    end = dividerLengths.TryGetValue(next, out var nextLength)
                        ? next.localPosition.z - nextLength * .5f
                        : next.localPosition.z - next.localScale.z * .5f;
                    if (next == firstPillar || next == secondPillar)
                        x = Random.Range(1.5f, 5f) * (Random.value < .5f ? -1f : 1f);
                }
                PlaceDivider(row, x, start, end, mix);
            }
        }

        private Transform Clone(Transform template, string name)
        {
            var clone = Instantiate(template, transform);
            clone.name = name;
            clone.gameObject.SetActive(true);
            return clone;
        }

        private static void Shuffle<T>(T[] items)
        {
            for (var i = items.Length - 1; i > 0; i--)
            {
                var pick = Random.Range(0, i + 1);
                (items[i], items[pick]) = (items[pick], items[i]);
            }
        }

        private Transform NewDivider(string name)
        {
            var divider = new GameObject(name).transform;
            divider.SetParent(transform, false);
            return divider;
        }

        private void PlaceDivider(Transform divider, float x, float start, float end, int mix)
        {
            var length = end - start;
            divider.localPosition = new Vector3(x, 0f, (start + end) * .5f);
            dividerLengths.Add(divider, length);

            var templates = new List<Transform>(3) { firstWall };
            if ((mix & 1) != 0) templates.Add(hurdle);
            if ((mix & 2) != 0) templates.Add(firstPillar);
            var pieces = templates.ToArray();
            Shuffle(pieces);

            var segmentLength = length / pieces.Length;
            for (var i = 0; i < pieces.Length; i++)
            {
                var template = pieces[i];
                var segment = Instantiate(template, divider);
                var segmentZ = -length * .5f + segmentLength * (i + .5f);
                if (template == firstPillar)
                {
                    segment.name = "Longitudinal Lift Gate";
                    segment.localScale = new Vector3(DividerThickness, PillarHeight, segmentLength);
                    segment.localPosition = new Vector3(0f, -PillarHeight * .5f, segmentZ);
                    dividerGates.Add((segment, new LiftCycle(Random.Range(0f, 2.8f))));
                }
                else
                {
                    var low = template == hurdle;
                    segment.name = low ? "Longitudinal Jump Barrier" : "Longitudinal Solid Wall";
                    Place(segment, 0f, segmentZ, DividerThickness,
                        low ? Random.Range(.42f, .57f) : Random.Range(1.8f, 2.2f), segmentLength);
                }
            }
        }

        private WallCombination CombineWall(Transform wall, float z)
        {
            const float laneWidth = HalfArenaWidth * 2f / 3f;
            var lanes = new[] { -laneWidth, 0f, laneWidth };
            Shuffle(lanes);

            var wallWidth = Random.Range(4f, 4.6f);
            var bypassSide = Random.value < .5f ? -1f : 1f;
            var wallX = lanes[0] - bypassSide * (laneWidth - wallWidth) * .5f;
            var bypassX = lanes[0] + bypassSide * wallWidth * .5f;
            Place(wall, wallX, z, wallWidth, Random.Range(1.8f, 2.2f), .75f);

            var low = Instantiate(hurdle, transform);
            low.name = "Wall Jump Barrier";
            Place(low, lanes[1], z, laneWidth, Random.Range(.42f, .57f), .35f);

            var gate = Instantiate(firstPillar, transform);
            gate.name = "Wall Lift Gate";
            gate.localScale = new Vector3(laneWidth, PillarHeight, .9f);
            gate.localPosition = new Vector3(lanes[2], -PillarHeight * .5f, z);
            return new WallCombination(low, gate, bypassX);
        }

        private static void PlaceSideHurdle(Transform obstacle, float z, float width) =>
            Place(obstacle, EdgeCenter(width, Random.value < .5f), z, width,
                Random.Range(.42f, .57f), Random.Range(.3f, .42f));

        public void AddFeatures(CourseFeatures features)
        {
            var route = BotRoute(0f, .38f, 31.95f);
            for (var i = 0; i < rows.Length; i++)
            {
                if (!detours.Contains(rows[i])) continue;
                var point = route[i * 2 + 1].Position;
                features.AddCheckpoint(new Vector3(point.x, .08f, point.y));
            }

            // Replace the middle mandatory wall with two solid flanks and a risky gap.
            var wall = rows[0];
            var detourIndex = 0;
            foreach (var row in rows)
                if (detours.Contains(row) && ++detourIndex == 2) { wall = row; break; }
            const float opening = 2.6f;
            var originalWidth = wall.localScale.x;

            var center = wall.position;
            var openingX = center.x + (Random.value < .5f ? -4f : 4f);
            foreach (var side in new[] { -1f, 1f })
            {
                var edge = center.x + side * originalWidth * .5f;
                var gapEdge = openingX + side * opening * .5f;
                var flank = Instantiate(wall, transform);
                flank.name = "Shortcut wall flank";
                Place(flank, (edge + gapEdge) * .5f, center.z,
                    Mathf.Abs(edge - gapEdge), wall.localScale.y, wall.localScale.z);
            }
            wall.GetComponent<Renderer>().enabled = false;
            wall.GetComponent<Collider>().enabled = false;
            var barrier = Instantiate(hurdle, transform);
            barrier.name = "Risky shortcut jump";
            Place(barrier, openingX, center.z, opening, .48f, .3f);
            features.AddRiskyPassage(new Rect(openingX - opening * .5f, center.z - 1.45f, opening, 1.3f), .65f);

            // Save progress before approaching either the shortcut or its long side detour.
            var before = route[System.Array.IndexOf(rows, wall) * 2].Position;
            features.AddCheckpoint(new Vector3(before.x, .08f, before.y));
        }

        public void TrackBots(CourseBot[] racers) => bots = racers;

        public CourseBot.Waypoint[] BotRoute(float spawnX, float lane, float finishZ)
        {
            var route = new CourseBot.Waypoint[rows.Length * 2 + 1];
            var targetXs = new float[rows.Length];
            for (var rowIndex = rows.Length - 1; rowIndex >= 0; rowIndex--)
            {
                var row = rows[rowIndex];
                var x = 0f;
                var jump = false;
                var approach = 1.1f;
                Transform gate = row == firstPillar || row == secondPillar ? row : null;
                if (combinations.TryGetValue(row, out var combination))
                {
                    jump = lane >= .45f && lane < .59f;
                    gate = lane >= .59f ? combination.Gate : null;
                    x = jump ? combination.Hurdle.position.x :
                        gate != null ? gate.position.x : combination.BypassX;
                }
                else if (row == centerBlock)
                {
                    var side = lane < .55f ? -1f : 1f;
                    var edge = row.position.x + side * row.localScale.x * .5f;
                    x = Mathf.Lerp(edge, side * HalfArenaWidth, .45f);
                }
                else if (dividerLengths.TryGetValue(row, out var length))
                {
                    approach = length * .5f + .3f;
                    x = rowIndex + 1 < rows.Length ? targetXs[rowIndex + 1] : 0f;
                    const float clearance = DividerThickness * .5f + .6f;
                    if (Mathf.Abs(x - row.position.x) < clearance)
                        x = row.position.x + (x < row.position.x ? -clearance : clearance);
                }
                else if (gate == null)
                {
                    var side = -Mathf.Sign(row.position.x);
                    x = side * Mathf.Lerp(row.localScale.x * .5f, HalfArenaWidth, lane);
                }

                targetXs[rowIndex] = x;
                var z = row.position.z;
                var exitZ = z + approach;
                if (dividerLengths.ContainsKey(row) && rowIndex + 1 < rows.Length)
                {
                    var next = rows[rowIndex + 1];
                    var nextApproach = dividerLengths.TryGetValue(next, out var nextLength) ? nextLength * .5f + .3f : 1.1f;
                    exitZ = Mathf.Min(exitZ, next.position.z - nextApproach);
                }
                route[rowIndex * 2] = new CourseBot.Waypoint(x, z - approach);
                route[rowIndex * 2 + 1] = new CourseBot.Waypoint(x, exitZ, jump, gate);
            }

            route[rows.Length * 2] = new CourseBot.Waypoint(targetXs[rows.Length - 1], finishZ + .45f);
            return route;
        }

        private void Update()
        {
            var time = Time.timeSinceLevelLoad;
            MovePillar(firstPillar, firstCycle.HeightAt(time));
            MovePillar(secondPillar, secondCycle.HeightAt(time));
            foreach (var combination in combinations.Values)
                if (combination.Gate != null)
                    MovePillar(combination.Gate, combination.Cycle.HeightAt(time));
            foreach (var dividerGate in dividerGates)
                MovePillar(dividerGate.Gate, dividerGate.Cycle.HeightAt(time));
        }

        private static void PlaceGate(Transform gate, float z)
        {
            gate.localScale = new Vector3(HalfArenaWidth * 2f, PillarHeight, Random.Range(.9f, 1.3f));
            gate.localPosition = new Vector3(0f, -PillarHeight * .5f, z);
        }

        private void MovePillar(Transform pillar, float height)
        {
            var position = pillar.localPosition;
            var targetY = height - PillarHeight * .5f;

            // Keep the gate from rising under a racer already crossing it.
            if (targetY > position.y && GateOccupied(pillar))
                return;

            position.y = targetY;
            pillar.localPosition = position;
        }

        private bool GateOccupied(Transform gate)
        {
            var center = gate.position;
            var halfWidth = gate.localScale.x * .5f + .32f;
            var halfDepth = gate.localScale.z * .5f + .32f;
            bool Contains(Vector3 position) =>
                Mathf.Abs(position.x - center.x) < halfWidth && Mathf.Abs(position.z - center.z) < halfDepth;

            if (Contains(player.position)) return true;

            if (bots != null)
                foreach (var bot in bots)
                    if (Contains(bot.Position)) return true;
            return false;
        }

        private readonly struct LiftCycle
        {
            private readonly float lowered;
            private readonly float raised;
            private readonly float transition;
            private readonly float offset;

            public LiftCycle(float offset)
            {
                lowered = Random.Range(1.8f, 2.5f);
                raised = Random.Range(.85f, 2.1f);
                transition = Random.Range(.45f, .8f);
                this.offset = offset;
            }

            public float HeightAt(float time)
            {
                var phase = Mathf.Repeat(time + offset, lowered + raised + 2f * transition);
                if (phase < lowered) return 0f;

                phase -= lowered;
                if (phase < transition)
                    return Mathf.SmoothStep(0f, PillarHeight, phase / transition);

                phase -= transition;
                if (phase < raised) return PillarHeight;

                return Mathf.SmoothStep(PillarHeight, 0f, (phase - raised) / transition);
            }
        }

        private static float EdgeCenter(float width, bool onLeft) =>
            (HalfArenaWidth - width * .5f) * (onLeft ? -1f : 1f);

        private static void Place(Transform obstacle, float x, float z, float width, float height, float depth)
        {
            obstacle.localScale = new Vector3(width, height, depth);
            obstacle.localPosition = new Vector3(x, height * .5f, z);
        }
    }
}
