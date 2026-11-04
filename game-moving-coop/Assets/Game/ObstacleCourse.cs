using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MovingCoop
{
    public sealed class ObstacleCourse : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform startLine;
        [SerializeField] private Transform finishLine;

        private const double Duration = 30;
        private enum CourseState { Ready, Running, Won, TimedOut }

        private CourseState state;
        private double deadline;
        private double finishedWith;
        private float previousZ;
        private GUIStyle statusStyle;

        private void Start() => previousZ = player.position.z;

        private void LateUpdate()
        {
            if (Application.isFocused && Keyboard.current?.rKey.wasPressedThisFrame == true)
            {
                SceneManager.LoadScene(gameObject.scene.buildIndex);
                return;
            }

            Advance(player.position.z, Time.timeAsDouble);
        }

        private void Advance(float z, double now)
        {
            if (state == CourseState.Ready && Crossed(startLine.position.z, z))
            {
                state = CourseState.Running;
                deadline = now + Duration;
            }

            if (state == CourseState.Running)
            {
                if (now >= deadline)
                    state = CourseState.TimedOut;
                else if (Crossed(finishLine.position.z, z))
                {
                    finishedWith = deadline - now;
                    state = CourseState.Won;
                }
            }

            previousZ = z;
        }

        private bool Crossed(float lineZ, float z) => previousZ <= lineZ && z > lineZ;

        private void OnGUI()
        {
            statusStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                wordWrap = true
            };

            var seconds = state switch
            {
                CourseState.Ready => Duration,
                CourseState.Running => System.Math.Max(0, deadline - Time.timeAsDouble),
                CourseState.Won => finishedWith,
                _ => 0
            };
            var message = state switch
            {
                CourseState.Ready => "Пересеки синюю линию, чтобы начать",
                CourseState.Running => "Доберись до зелёной линии",
                CourseState.Won => "Победа!  R — заново",
                _ => "Время вышло.  R — заново"
            };
            var width = Mathf.Min(410f, Screen.width - 16f);
            var box = new Rect((Screen.width - width) / 2f, 12f, width, 68f);
            GUI.Box(box, GUIContent.none);
            GUI.Label(box, $"{seconds:00.0} сек\n{message}", statusStyle);
        }
    }
}
