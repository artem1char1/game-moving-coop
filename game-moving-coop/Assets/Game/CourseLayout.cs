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
        private const float PillarHeight = 1.9f;
        private const float PillarWidth = 1.2f;
        private const float LiftPeriod = 4f;
        private static bool? previousLeftWall;

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
                    var offset = row == sideHurdle ? .5f : lane;
                    x = side * Mathf.Lerp(gapCenter, HalfArenaWidth, offset);
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
            var phase = Time.timeSinceLevelLoad / LiftPeriod;
            MovePillar(firstPillar, phase);
            MovePillar(secondPillar, phase + .5f);
        }

        private static void PlacePillar(Transform pillar, Transform wall)
        {
            var gapCenter = -Mathf.Sign(wall.localPosition.x) * wall.localScale.x * .5f;
            pillar.localScale = new Vector3(PillarWidth, PillarHeight, PillarWidth);
            pillar.localPosition = new Vector3(gapCenter, -PillarHeight * .5f, wall.localPosition.z);
        }

        private void MovePillar(Transform pillar, float phase)
        {
            var lift = .5f - .5f * Mathf.Cos(phase * 2f * Mathf.PI);
            var height = PillarHeight * Mathf.Clamp01((lift - .15f) / .7f);
            var position = pillar.localPosition;
            var targetY = height - PillarHeight * .5f;

            // Hold the current height while a player is over the footprint.
            var offset = player.position - pillar.position;
            var clearance = PillarWidth * .5f + .32f;
            if (targetY > position.y && Mathf.Abs(offset.x) < clearance && Mathf.Abs(offset.z) < clearance)
                return;

            position.y = targetY;
            pillar.localPosition = position;
        }

        private static void PlaceWall(Transform wall, float z, bool onLeft)
        {
            var width = Random.Range(12f, 16f);
            Place(wall, EdgeCenter(width, onLeft), z, width, Random.Range(1.8f, 2.2f),
                Random.Range(.55f, .75f));
        }

        private static float Lane(float z) => z + Random.Range(-.22f, .22f);

        private static float EdgeCenter(float width, bool onLeft) =>
            (HalfArenaWidth - width * .5f) * (onLeft ? -1f : 1f);

        private static void Place(Transform obstacle, float x, float z, float width, float height, float depth)
        {
            obstacle.localScale = new Vector3(width, height, depth);
            obstacle.localPosition = new Vector3(x, height * .5f, z);
        }
    }
}
