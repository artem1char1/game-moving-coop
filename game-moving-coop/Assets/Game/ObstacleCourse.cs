using System.Collections.Generic;
using System.Text;
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

        private enum CourseState { Ready, Running, Complete }

        private CourseState state;
        private double startedAt;
        private double endedAt;
        private double previousTime;
        private readonly CourseBot[] bots = new CourseBot[3];
        private readonly bool[] finished = new bool[4];
        private readonly List<(int Racer, double Time)> results = new(4);
        private readonly List<(int Racer, double Time)> crossings = new(4);
        private readonly GUIContent resultsText = new();
        private static readonly string[] RacerNames = { "Ты", "Бот 1", "Бот 2", "Бот 3" };
        private float previousZ;
        private GUIStyle statusStyle;
        private GUIStyle controlsTitleStyle;
        private GUIStyle controlsTextStyle;

        private static readonly GUIContent ControlsTitle = new("Управление");
        private GUIContent controlsText;

        private void Start()
        {
            previousZ = player.position.z;
            previousTime = Time.timeAsDouble;
            var layout = GetComponent<CourseLayout>();
            var playerCollider = player.GetComponent<CharacterController>();
            var colors = new[] { new Color(.95f, .25f, .31f), new Color(.98f, .76f, .15f), new Color(.25f, .55f, 1f) };
            var speeds = new[] { 3.35f, 3.75f, 4.1f };
            for (var i = 0; i < bots.Length; i++)
            {
                var spawn = player.position + Vector3.right * ((i - 1.5f) * 1.35f);
                bots[i] = new CourseBot(transform, RacerNames[i + 1], spawn, colors[i], speeds[i],
                    layout.BotRoute(spawn.x, .38f + i * .14f, finishLine.position.z), playerCollider, bots);
            }
            UpdateResults();
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
            if (state == CourseState.Ready && previousZ <= startLine.position.z && z > startLine.position.z)
            {
                state = CourseState.Running;
                startedAt = now;
            }

            if (state == CourseState.Running)
            {
                crossings.Clear();
                RecordCrossing(0, previousZ, z, now);
                for (var i = 0; i < bots.Length; i++)
                {
                    if (finished[i + 1]) continue;
                    var before = bots[i].Z;
                    bots[i].Step((float)System.Math.Clamp(now - System.Math.Max(previousTime, startedAt), 0d, .1d));
                    RecordCrossing(i + 1, before, bots[i].Z, now);
                }

                crossings.Sort((a, b) =>
                {
                    var timeOrder = a.Time.CompareTo(b.Time);
                    return timeOrder != 0 ? timeOrder : a.Racer.CompareTo(b.Racer);
                });
                foreach (var crossing in crossings)
                {
                    finished[crossing.Racer] = true;
                    results.Add(crossing);
                }

                if (results.Count == finished.Length)
                {
                    state = CourseState.Complete;
                    endedAt = now;
                    UpdateResults();
                }
                else if (crossings.Count > 0)
                    UpdateResults();
            }

            previousZ = z;
            previousTime = now;
        }

        private void RecordCrossing(int racer, float from, float to, double now)
        {
            var line = finishLine.position.z;
            if (finished[racer] || from > line || to <= line) return;

            var fraction = (line - from) / (to - from);
            var time = System.Math.Max(startedAt, previousTime);
            time += (now - time) * fraction;
            crossings.Add((racer, time - startedAt));
        }

        private void UpdateResults()
        {
            var text = new StringBuilder("Результаты");
            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                text.Append($"\n{i + 1}. {RacerNames[result.Racer]} — {result.Time:0.00} сек");
            }

            for (var i = 0; i < RacerNames.Length; i++)
            {
                if (finished[i]) continue;
                text.Append($"\n{RacerNames[i]} —");
            }
            resultsText.text = text.ToString();
        }
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
                CourseState.Ready => 0,
                CourseState.Running => Time.timeAsDouble - startedAt,
                _ => endedAt - startedAt
            };
            var playerPlace = results.FindIndex(result => result.Racer == 0) + 1;
            var message = state switch
            {
                CourseState.Ready => "Пересеки синюю линию, чтобы начать гонку",
                _ when playerPlace == 1 => "Победа!  R — заново",
                _ when playerPlace > 1 => $"Твоё место: {playerPlace} из 4.  R — заново",
                _ => "Доберись до зелёной линии"
            };
            var width = Mathf.Min(410f, Screen.width - 16f);
            var box = new Rect((Screen.width - width) / 2f, 12f, width, 68f);
            GUI.Box(box, GUIContent.none);
            GUI.Label(box, $"{seconds:00.0} сек\n{message}", statusStyle);
            DrawControls();
            DrawResults();
        }

        private void DrawResults()
        {
            controlsTextStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            const float padding = 10f;
            var width = Mathf.Min(215f, Screen.width - 24f);
            var innerWidth = width - 2f * padding;
            var textHeight = controlsTextStyle.CalcHeight(resultsText, innerWidth);
            var x = Screen.width - width - 12f;
            const float top = 88f;
            GUI.Box(new Rect(x, top, width, textHeight + 2f * padding), GUIContent.none);
            GUI.Label(new Rect(x + padding, top + padding, innerWidth, textHeight), resultsText, controlsTextStyle);
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
