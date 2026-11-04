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
        private GUIStyle controlsTitleStyle;
        private GUIStyle controlsTextStyle;

        private static readonly GUIContent ControlsTitle = new("Управление");
        private GUIContent controlsText;

        private void Start()
        {
            previousZ = player.position.z;
            controlsText = new GUIContent(player.GetComponent<PlayerMovement>().Controls.Describe());
        }

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
            DrawControls();
        }

        private void DrawControls()
        {
            controlsTitleStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16
            };
            controlsTextStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true
            };

            const float margin = 12f;
            const float padding = 10f;
            var width = Mathf.Min(244f, Screen.width - margin * 2f);
            var innerWidth = width - padding * 2f;
            var titleHeight = controlsTitleStyle.CalcHeight(ControlsTitle, innerWidth);
            var textHeight = controlsTextStyle.CalcHeight(controlsText, innerWidth);
            var height = titleHeight + textHeight + padding * 2f;
            var top = Screen.height - margin - height;

            GUI.Box(new Rect(margin, top, width, height), GUIContent.none);
            GUI.Label(new Rect(margin + padding, top + padding, innerWidth, titleHeight),
                ControlsTitle, controlsTitleStyle);
            GUI.Label(new Rect(margin + padding, top + padding + titleHeight, innerWidth, textHeight),
                controlsText, controlsTextStyle);
        }
    }
}
