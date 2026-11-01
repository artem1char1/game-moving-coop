using System.Linq;
using NUnit.Framework;
using MovingCoop.UnityAdapters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace MovingCoop.Tests
{
    public sealed class UnityAdapterTests
    {
        [Test]
        public void KeyboardEventsUseScreenDirectionsAndNormalizeDiagonals()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); InputSystem.Update();
                var cardinal=LocalInput.Read(keyboard,true); Assert.AreEqual(Vector2.up,cardinal);
                var cameraRotation=Quaternion.Euler(50,45,0);
                var forward=cameraRotation*Vector3.forward;forward.y=0;forward.Normalize();
                var world=LocalInput.ToWorld(cardinal);
                Assert.That(Vector2.Distance(world,new Vector2(forward.x,forward.z)),Is.LessThan(.0001f));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));InputSystem.Update();
                var right=LocalInput.ToWorld(LocalInput.Read(keyboard,true));
                Assert.That(right.x,Is.GreaterThan(0));Assert.That(right.y,Is.LessThan(0));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D));InputSystem.Update();
                var diagonal=LocalInput.ToWorld(LocalInput.Read(keyboard,true));
                Assert.That(diagonal.magnitude,Is.EqualTo(world.magnitude).Within(.0001f));
                Assert.That(diagonal.x,Is.EqualTo(1).Within(.0001f));Assert.That(diagonal.y,Is.EqualTo(0).Within(.0001f));
                Assert.AreEqual(Vector2.zero,LocalInput.Read(keyboard,false));
            }
            finally {InputSystem.RemoveDevice(keyboard);}
        }
        [Test]
        public void AuthoredSceneCameraHidesAndRestoresWallsForPlayerAndCargo()
        {
            const string scenePath="Assets/Game/Scenes/Iteration01.unity";
            var scene=SceneManager.GetSceneByPath(scenePath);
            bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            var target=new GameObject("TestOnlyCameraTarget");
            var rig=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<IsometricCamera>()).Single();
            var oldPosition=rig.transform.position;var oldRotation=rig.transform.rotation;var oldTarget=rig.Target;
            var oldPoints=rig.CargoOcclusionPoints.ToArray();
            try
            {
                Physics.SyncTransforms();
                var walls=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WallOccluder>()).ToArray();
                target.transform.position=new Vector3(-2,.75f,3.4f);rig.Target=target.transform;rig.UpdateView(100);
                Assert.That(Vector3.Distance(rig.transform.position,target.transform.position-rig.transform.forward*17),Is.LessThan(.001f));
                var hidden=walls.Where(w=>!w.GetComponent<Renderer>().enabled).ToArray();Assert.IsNotEmpty(hidden);
                Assert.IsTrue(hidden.All(w=>w.GetComponent<Collider>().enabled));
                target.transform.position=new Vector3(0,.75f,-8);rig.UpdateView(100);
                Assert.IsTrue(hidden.All(w=>w.GetComponent<Renderer>().enabled));
                rig.CargoOcclusionPoints.Add(new Vector3(-2,.7f,3.4f));rig.UpdateView(100);
                var cargoHidden=walls.Where(w=>!w.GetComponent<Renderer>().enabled).ToArray();Assert.IsNotEmpty(cargoHidden);
                Assert.IsTrue(cargoHidden.All(w=>w.GetComponent<Collider>().enabled));
                rig.CargoOcclusionPoints.Clear();rig.UpdateView(100);
                Assert.IsTrue(cargoHidden.All(w=>w.GetComponent<Renderer>().enabled));
            }
            finally {rig.Target=null;rig.CargoOcclusionPoints.Clear();rig.UpdateView(0);rig.transform.position=oldPosition;rig.transform.rotation=oldRotation;rig.Target=oldTarget;rig.CargoOcclusionPoints.AddRange(oldPoints);Object.DestroyImmediate(target);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
