using UnityEngine;

namespace MovingCoop
{
    public sealed class CourseLayout : MonoBehaviour
    {
        [SerializeField] private Transform hurdle;
        [SerializeField] private Transform secondHurdle;
        [SerializeField] private Transform centerBlock;
        [SerializeField] private Transform firstWall;
        [SerializeField] private Transform secondWall;
        [SerializeField] private Transform sideHurdle;
        [SerializeField] private Transform extraHurdle;
        [SerializeField] private Transform extraWall;
        [SerializeField] private Transform firstPillar;
        [SerializeField] private Transform secondPillar;
        [SerializeField] private Transform player;

        private const float HalfArenaWidth = 11f;
        private const float LengthScale = 3f;
        private const float PillarHeight = 1.9f;
        private static bool? previousLeftWall;
        private LiftCycle firstCycle;
        private LiftCycle secondCycle;
        private CourseBot[] bots;
        private WallCombination firstCombination;
        private WallCombination secondCombination;

        private enum WallMix { Jump, Lift, Both }

        private readonly struct WallCombination
        {
            public readonly Transform Hurdle;
            public readonly Transform Gate;
            public readonly LiftCycle Cycle;

            public WallCombination(Transform hurdle, Transform gate)
            {
                Hurdle = hurdle;
                Gate = gate;
                Cycle = new LiftCycle(Random.Range(0f, 2.8f));
            }
        }

        private void Awake()
        {
            // Alternate the first detour so successive attempts always change the route.
            var firstWallOnLeft = previousLeftWall.HasValue ? !previousLeftWall.Value : Random.value < .5f;
            previousLeftWall = firstWallOnLeft;

            var jumpFirst = Random.value < .5f;
            var jumpLast = Random.value < .5f;
            PlaceHurdle(hurdle, jumpFirst ? -8f : -4.8f);
            PlaceHurdle(secondHurdle, jumpLast ? 8f : 4.8f);
            PlaceWall(firstWall, Lane(-1.6f), firstWallOnLeft);
            PlaceWall(secondWall, Lane(1.6f), !firstWallOnLeft);

            PlaceGate(firstPillar, Lane(-6.4f));
            PlaceGate(secondPillar, Lane(6.4f));
            firstCycle = new LiftCycle(0f);
            secondCycle = new LiftCycle(Random.Range(1.2f, 2.8f));

            var extraWallFirst = Random.value < .5f;
            PlaceWall(extraWall, Lane(extraWallFirst ? -3.2f : 3.2f), !firstWallOnLeft);
            PlaceSideHurdle(extraHurdle, Lane(extraWallFirst ? 3.2f : -3.2f));

            var earlySlot = jumpFirst ? -4.8f : -8f;
            var lateSlot = jumpLast ? 4.8f : 8f;
            var blockFirst = Random.value < .5f;
            Place(centerBlock, Random.Range(-2f, 2f), Lane(blockFirst ? earlySlot : lateSlot),
                Random.Range(4f, 7f), Random.Range(1.7f, 2.15f), Random.Range(.8f, 1.2f));

            PlaceSideHurdle(sideHurdle, Lane(blockFirst ? lateSlot : earlySlot));

            var firstMix = Random.Range(0, 3);
            var secondMix = (firstMix + Random.Range(1, 3)) % 3;
            firstCombination = CombineWall(firstWall, (WallMix)firstMix);
            secondCombination = CombineWall(secondWall, (WallMix)secondMix);
        }

        private WallCombination CombineWall(Transform wall, WallMix mix)
        {
            var side = -Mathf.Sign(wall.localPosition.x);
            var wallEdge = wall.localPosition.x + side * wall.localScale.x * .5f;
            var gapWidth = Mathf.Abs(side * HalfArenaWidth - wallEdge);
            var split = mix == WallMix.Both;
            var width = split ? gapWidth * .5f : gapWidth;
            var near = wallEdge + side * width * .5f;
            var far = wallEdge + side * width * 1.5f;
            if (split && Random.value < .5f) (near, far) = (far, near);

            Transform low = null;
            Transform gate = null;
            if (mix != WallMix.Lift)
            {
                low = Instantiate(hurdle, transform);
                low.name = "Wall Jump Barrier";
                Place(low, near, wall.localPosition.z, width, Random.Range(.42f, .57f), .35f);
            }

            if (mix != WallMix.Jump)
            {
                gate = Instantiate(firstPillar, transform);
                gate.name = "Wall Lift Gate";
                gate.localScale = new Vector3(width, PillarHeight, .9f);
                gate.localPosition = new Vector3(split ? far : near, -PillarHeight * .5f, wall.localPosition.z);
            }

            return new WallCombination(low, gate);
        }

        private static void PlaceHurdle(Transform obstacle, float z) =>
            Place(obstacle, 0f, Lane(z), HalfArenaWidth * 2f, Random.Range(.42f, .57f), Random.Range(.3f, .42f));

        private static void PlaceSideHurdle(Transform obstacle, float z)
        {
            var width = Random.Range(6f, 10f);
            Place(obstacle, EdgeCenter(width, Random.value < .5f), z, width,
                Random.Range(.42f, .57f), Random.Range(.3f, .42f));
        }

        internal void TrackBots(CourseBot[] racers) => bots = racers;

        internal CourseBot.Waypoint[] BotRoute(float spawnX, float lane, float finishZ)
        {
            var rows = new[]
            {
                hurdle, secondHurdle, firstWall, secondWall, sideHurdle, centerBlock,
                firstPillar, secondPillar, extraHurdle, extraWall
            };
            System.Array.Sort(rows, (a, b) => a.position.z.CompareTo(b.position.z));
            var route = new CourseBot.Waypoint[rows.Length * 2 + 1];
            var x = spawnX;
            var index = 0;
            foreach (var row in rows)
            {
                var jump = row == hurdle || row == secondHurdle;
                Transform gate = row == firstPillar || row == secondPillar ? row : null;
                if (row == firstWall || row == secondWall)
                {
                    var combination = row == firstWall ? firstCombination : secondCombination;
                    jump = combination.Hurdle != null && (combination.Gate == null || lane < .55f);
                    gate = jump ? null : combination.Gate;
                    x = (jump ? combination.Hurdle : gate).position.x;
                }
                else if (row == centerBlock)
                {
                    var side = lane < .55f ? -1f : 1f;
                    var edge = row.position.x + side * row.localScale.x * .5f;
                    x = Mathf.Lerp(edge, side * HalfArenaWidth, .45f);
                }
                else if (!jump && gate == null)
                {
                    var side = -Mathf.Sign(row.position.x);
                    x = side * Mathf.Lerp(row.localScale.x * .5f, HalfArenaWidth, lane);
                }

                var z = row.position.z;
                route[index++] = new CourseBot.Waypoint(x, z - 1.1f);
                route[index++] = new CourseBot.Waypoint(x, z + 1.1f, jump, gate);
            }

            route[index] = new CourseBot.Waypoint(x, finishZ + .45f);
            return route;
        }

        private void Update()
        {
            var time = Time.timeSinceLevelLoad;
            MovePillar(firstPillar, firstCycle.HeightAt(time));
            MovePillar(secondPillar, secondCycle.HeightAt(time));
            if (firstCombination.Gate != null)
                MovePillar(firstCombination.Gate, firstCombination.Cycle.HeightAt(time));
            if (secondCombination.Gate != null)
                MovePillar(secondCombination.Gate, secondCombination.Cycle.HeightAt(time));
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

        private static void PlaceWall(Transform wall, float z, bool onLeft)
        {
            var width = Random.Range(12f, 16f);
            Place(wall, EdgeCenter(width, onLeft), z, width, Random.Range(1.8f, 2.2f),
                Random.Range(.55f, .75f));
        }

        private static float Lane(float z) => z * LengthScale + Random.Range(-.22f, .22f);

        private static float EdgeCenter(float width, bool onLeft) =>
            (HalfArenaWidth - width * .5f) * (onLeft ? -1f : 1f);

        private static void Place(Transform obstacle, float x, float z, float width, float height, float depth)
        {
            obstacle.localScale = new Vector3(width, height, depth);
            obstacle.localPosition = new Vector3(x, height * .5f, z);
        }
    }
}
