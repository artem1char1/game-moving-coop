using UnityEngine;

namespace MovingCoop
{
    internal interface ICourseMap
    {
        CourseBot.Waypoint[] BotRoute(float spawnX, float lane, float finishZ);
        void TrackBots(CourseBot[] racers);
        void AddFeatures(CourseFeatures features);
    }

    public enum MapChoice { Labyrinth, Obstacles, Random }

    public sealed class CourseLayout : MonoBehaviour
    {
        [SerializeField] private Transform firstWall;
        [SerializeField] private Transform hurdle;
        [SerializeField] private Transform firstPillar;
        [SerializeField] private Transform player;

        public static MapChoice Selection { get; set; } = MapChoice.Random;
        public bool Ready => map != null;
        public MapChoice ActualMode { get; private set; }
        private ICourseMap map;

        private void Awake()
        {
            foreach (Transform child in transform)
                if (child.name != "START" && child.name != "FINISH")
                    child.gameObject.SetActive(false);
            SetPlayerActive(false);
        }

        public void Begin()
        {
            if (Ready) return;
            ActualMode = Selection == MapChoice.Random
                ? (MapChoice)UnityEngine.Random.Range(0, 2) : Selection;
            if (ActualMode == MapChoice.Labyrinth)
            {
                var generator = gameObject.AddComponent<MazeGenerator>();
                generator.Initialize(firstWall, hurdle, firstPillar, player);
                map = generator;
            }
            else
            {
                var generator = gameObject.AddComponent<ObstacleGenerator>();
                generator.Initialize(firstWall, hurdle, firstPillar, player);
                map = generator;
            }
            var features = gameObject.AddComponent<CourseFeatures>();
            features.Initialize(player.GetComponent<PlayerMovement>(), firstPillar.GetComponent<Renderer>().sharedMaterial);
            map.AddFeatures(features);
            features.BuildTerrain();
            SetPlayerActive(true);
        }

        private void SetPlayerActive(bool active)
        {
            player.GetComponent<PlayerMovement>().enabled = active;
            player.GetComponentInChildren<MouseOrbitCamera>().enabled = active;
        }

        internal void TrackBots(CourseBot[] racers) => map.TrackBots(racers);
        internal CourseBot.Waypoint[] BotRoute(float spawnX, float lane, float finishZ) =>
            map.BotRoute(spawnX, lane, finishZ);
    }
}
