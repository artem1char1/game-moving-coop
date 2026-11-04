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
        [SerializeField] private Transform firstPillar;
        [SerializeField] private Transform secondPillar;
        [SerializeField] private Transform player;

        private const float HalfArenaWidth = 11f;
        private const float LengthScale = 3f;
        private const float PillarHeight = 1.9f;
        private static bool? previousLeftWall;
        private LiftCycle firstCycle;
        private LiftCycle secondCycle;

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

            PlacePillar(firstPillar, firstWall);
            PlacePillar(secondPillar, secondWall);
            firstCycle = new LiftCycle(0f);
            secondCycle = new LiftCycle(Random.Range(1.2f, 2.8f));

            var earlySlot = jumpFirst ? -4.8f : -8f;
            var lateSlot = jumpLast ? 4.8f : 8f;
            var blockFirst = Random.value < .5f;
            Place(centerBlock, Random.Range(-2f, 2f), Lane(blockFirst ? earlySlot : lateSlot),
                Random.Range(4f, 7f), Random.Range(1.7f, 2.15f), Random.Range(.8f, 1.2f));

            var sideWidth = Random.Range(6f, 10f);
            Place(sideHurdle, EdgeCenter(sideWidth, Random.value < .5f), Lane(blockFirst ? lateSlot : earlySlot),
                sideWidth, Random.Range(.42f, .57f), Random.Range(.3f, .42f));
        }

        private static void PlaceHurdle(Transform obstacle, float z) =>
            Place(obstacle, 0f, Lane(z), HalfArenaWidth * 2f, Random.Range(.42f, .57f), Random.Range(.3f, .42f));

        internal CourseBot.Waypoint[] BotRoute(float spawnX, float lane, float finishZ)
        {
            var rows = new[] { hurdle, secondHurdle, firstWall, secondWall, sideHurdle, centerBlock };
            System.Array.Sort(rows, (a, b) => a.position.z.CompareTo(b.position.z));
            var route = new CourseBot.Waypoint[rows.Length * 2 + 1];
            var x = spawnX;
            var index = 0;
            foreach (var row in rows)
            {
                var jump = row == hurdle || row == secondHurdle;
                if (row == centerBlock)
                {
                    var side = lane < .55f ? -1f : 1f;
                    var edge = row.position.x + side * row.localScale.x * .5f;
                    x = Mathf.Lerp(edge, side * HalfArenaWidth, .45f);
                }
                else if (!jump)
                {
                    var side = -Mathf.Sign(row.position.x);
                    var gapCenter = row.localScale.x * .5f;
                    if (row == sideHurdle)
                        x = side * Mathf.Lerp(gapCenter, HalfArenaWidth, .5f);
                    else
                    {
                        var pillar = row == firstWall ? firstPillar : secondPillar;
                        var outerEdge = gapCenter + pillar.localScale.x * .5f;
                        x = side * Mathf.Lerp(outerEdge, HalfArenaWidth, lane);
                    }
                }

                var z = row.position.z;
                route[index++] = new CourseBot.Waypoint(x, z - 1.1f);
                route[index++] = new CourseBot.Waypoint(x, z + 1.1f, jump);
            }

            route[index] = new CourseBot.Waypoint(x, finishZ + .45f);
            return route;
        }

        private void Update()
        {
            var time = Time.timeSinceLevelLoad;
            MovePillar(firstPillar, firstCycle.HeightAt(time));
            MovePillar(secondPillar, secondCycle.HeightAt(time));
        }

        private static void PlacePillar(Transform pillar, Transform wall)
        {
            var gapCenter = -Mathf.Sign(wall.localPosition.x) * wall.localScale.x * .5f;
            pillar.localScale = new Vector3(Random.Range(1.35f, 3.2f), PillarHeight, 1.2f);
            pillar.localPosition = new Vector3(gapCenter, -PillarHeight * .5f, wall.localPosition.z);
        }

        private void MovePillar(Transform pillar, float height)
        {
            var position = pillar.localPosition;
            var targetY = height - PillarHeight * .5f;

            // Hold the current height while a player is over the footprint.
            var offset = player.position - pillar.position;
            var size = pillar.localScale;
            if (targetY > position.y && Mathf.Abs(offset.x) < size.x * .5f + .32f &&
                Mathf.Abs(offset.z) < size.z * .5f + .32f)
                return;

            position.y = targetY;
            pillar.localPosition = position;
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
