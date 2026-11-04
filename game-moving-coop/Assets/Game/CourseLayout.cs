using UnityEngine;

namespace MovingCoop
{
    public sealed class CourseLayout : MonoBehaviour
    {
        [SerializeField] private Transform hurdle;
        [SerializeField] private Transform firstWall;
        [SerializeField] private Transform secondWall;
        [SerializeField] private Transform sideHurdle;

        private const float HalfArenaWidth = 11f;
        private static bool? previousLeftWall;

        private void Awake()
        {
            // Alternate the first detour so successive attempts always change the route.
            var firstWallOnLeft = previousLeftWall.HasValue ? !previousLeftWall.Value : Random.value < .5f;
            previousLeftWall = firstWallOnLeft;

            var jumpFirst = Random.value < .5f;
            Place(hurdle, 0f, Lane(jumpFirst ? -4.1f : 4.45f), HalfArenaWidth * 2f,
                Random.Range(.42f, .57f), Random.Range(.3f, .42f));
            PlaceWall(firstWall, Lane(-1.25f), firstWallOnLeft);
            PlaceWall(secondWall, Lane(1.6f), !firstWallOnLeft);

            var sideWidth = Random.Range(6f, 10f);
            Place(sideHurdle, EdgeCenter(sideWidth, Random.value < .5f), Lane(jumpFirst ? 4.45f : -4.1f),
                sideWidth, Random.Range(.42f, .57f), Random.Range(.3f, .42f));
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
