using UnityEngine;

namespace MovingCoop
{
    internal sealed class MouseCircleGesture
    {
        private const float MaxDuration = 2.5f;
        private const float MaxPause = 0.4f;
        private const float SampleSpacing = 5f;
        private const float MinimumSpan = 17f;
        private const int Capacity = 384;

        private readonly (Vector2 Position, float Time)[] points = new (Vector2, float)[Capacity];
        private int next;
        private int count;

        public bool Sample(Vector2 position, float now)
        {
            if (count > 0 && now - Point(0).Time > MaxPause)
                Reset();

            if (count > 0 && (position - Point(0).Position).sqrMagnitude < SampleSpacing * SampleSpacing)
                return false;

            points[next] = (position, now);
            next = (next + 1) % Capacity;
            count = Mathf.Min(count + 1, Capacity);

            var min = position;
            var max = position;
            var previous = position;
            var travel = 0f;
            var twiceArea = 0f;

            // Try recent starting points so approaching the circle does not spoil the gesture.
            for (var age = 1; age < count; age++)
            {
                var point = Point(age);
                if (now - point.Time > MaxDuration) break;

                var segment = previous - point.Position;
                travel += segment.magnitude;
                var from = point.Position - position;
                var to = previous - position;
                twiceArea += from.x * to.y - from.y * to.x;

                min = Vector2.Min(min, point.Position);
                max = Vector2.Max(max, point.Position);
                var span = max - min;
                // A closed path needs appreciable enclosed area, unlike a line or a scribble.
                if (age >= 7 && span.x >= MinimumSpan && span.y >= MinimumSpan &&
                    2f * Mathf.PI * Mathf.Abs(twiceArea) >= 0.45f * travel * travel &&
                    (position - point.Position).magnitude <= travel * 0.20f)
                {
                    Reset();
                    return true;
                }

                previous = point.Position;
            }

            return false;
        }

        private (Vector2 Position, float Time) Point(int age) =>
            points[(next - age - 1 + Capacity) % Capacity];

        public void Reset() => count = 0;
    }
}
