#if UNITY_EDITOR
using System.IO;
using MovingCoop.Bootstrap;
using MovingCoop.UnityAdapters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MovingCoop.EditorTools
{
    public static class IterationSceneAuthoring
    {
        public const string ScenePath = "Assets/Game/Scenes/Iteration01.unity";
        public static string BuildCurrentScene()
        {
            // Called via registered Pipeline script eval after scene create; never hand-edit YAML.
            if (EditorSceneManager.GetActiveScene().path != ScenePath) return "Refused: wrong scene";
            if (GameObject.Find("Iteration01") != null) return "Refused: scene already authored";
            var root = new GameObject("Iteration01");
            var floor = new Color(0.62f, 0.67f, 0.64f);
            Box(root.transform, "Yard", new Vector3(0, -0.15f, -5.5f), new Vector3(22, 0.3f, 9), new Color(0.22f, 0.35f, 0.33f));
            Box(root.transform, "Corridor", new Vector3(0, -0.15f, 1), new Vector3(18, 0.3f, 4), floor);
            var colors = new[] { new Color(0.65f, 0.48f, 0.38f), new Color(0.57f, 0.66f, 0.55f), new Color(0.48f, 0.58f, 0.69f) };
            for (var i = 0; i < 3; i++)
            {
                var x = -6 + i * 6;
                Box(root.transform, "Room_" + (i + 1), new Vector3(x, -0.15f, 6), new Vector3(6, 0.3f, 6), colors[i]);
                Wall(root.transform, "DoorLeft_" + i, new Vector3(x - 1.95f, 1.3f, 3), new Vector3(2.1f, 2.6f, 0.18f));
                Wall(root.transform, "DoorRight_" + i, new Vector3(x + 1.95f, 1.3f, 3), new Vector3(2.1f, 2.6f, 0.18f));
                // Placeholders are deliberately not interactable yet. Nine boxes and one sofa are next batch.
                Label(root.transform, "RoomLabel_" + i, new Vector3(x, 0.025f, 6), "ROOM 0" + (i + 1), Color.white);
            }
            Wall(root.transform, "BackWall", new Vector3(0, 1.3f, 9), new Vector3(18.2f, 2.6f, 0.18f));
            Wall(root.transform, "LeftWall", new Vector3(-9, 1.3f, 4), new Vector3(0.18f, 2.6f, 10));
            Wall(root.transform, "RightWall", new Vector3(9, 1.3f, 4), new Vector3(0.18f, 2.6f, 10));
            foreach (var x in new[] { -3, 3 }) Wall(root.transform, "Partition_" + x, new Vector3(x, 1.3f, 6), new Vector3(0.18f, 2.6f, 6));
            Wall(root.transform, "FrontLeft", new Vector3(-5.1f, 1.3f, -1), new Vector3(7.8f, 2.6f, 0.18f));
            Wall(root.transform, "FrontRight", new Vector3(5.1f, 1.3f, -1), new Vector3(7.8f, 2.6f, 0.18f));
            var zone = Box(root.transform, "DeliveryZone", new Vector3(0, 0.015f, -6), new Vector3(4, 0.03f, 3), new Color(0.92f, 0.74f, 0.28f));
            Object.DestroyImmediate(zone.GetComponent<Collider>());
            Label(root.transform, "DeliveryLabel", new Vector3(0, 0.045f, -6), "DELIVERY", new Color(0.15f, 0.2f, 0.2f));
            var van = Box(root.transform, "MovingVan", new Vector3(4.3f, 1.15f, -6), new Vector3(2.8f, 2.3f, 5), new Color(0.84f, 0.9f, 0.87f));
            van.layer = PlayerWorld.ObstacleLayer;
            Box(root.transform, "VanCab", new Vector3(4.3f, 0.9f, -9), new Vector3(2.8f, 1.8f, 1), new Color(0.12f, 0.4f, 0.46f)).layer = PlayerWorld.ObstacleLayer;
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(IsometricCamera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(root.transform);
            cameraObject.transform.rotation = Quaternion.Euler(50, 45, 0);
            cameraObject.transform.position = new Vector3(-12, 14, -18);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 8; camera.nearClipPlane = 0.1f; camera.farClipPlane = 80;
            camera.backgroundColor = new Color(0.08f, 0.14f, 0.16f); camera.clearFlags = CameraClearFlags.SolidColor;
            var light = new GameObject("Sun", typeof(Light)); light.transform.SetParent(root.transform);
            light.transform.rotation = Quaternion.Euler(55, -25, 0); light.GetComponent<Light>().type = LightType.Directional; light.GetComponent<Light>().intensity = 1.4f;
            RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.7f);
            new GameObject("CompositionRoot", typeof(GameBootstrap)).transform.SetParent(root.transform);
            UnityEngine.Application.runInBackground = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            return "Authored and saved " + ScenePath;
        }
        private static Material Material(string name, Color color)
        {
            var dir = "Assets/Game/Art/Materials";
            Directory.CreateDirectory(dir);
            var path = dir + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static GameObject Box(Transform parent, string name, Vector3 at, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent); go.transform.position = at; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Material(name, color); return go;
        }
        private static void Wall(Transform parent, string name, Vector3 at, Vector3 size)
        {
            var go = Box(parent, name, at, size, new Color(0.25f, 0.42f, 0.43f));
            go.layer = PlayerWorld.ObstacleLayer; go.AddComponent<WallOccluder>();
        }
        private static void Label(Transform parent, string name, Vector3 at, string text, Color color)
        {
            var go = new GameObject(name, typeof(TextMesh)); go.transform.SetParent(parent); go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            var mesh = go.GetComponent<TextMesh>(); mesh.text = text; mesh.fontSize = 32; mesh.characterSize = 0.18f;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.color = color;
        }
    }
}
#endif
