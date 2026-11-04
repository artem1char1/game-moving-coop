using UnityEngine;

namespace MovingCoop
{
    internal sealed class MouseCircleGesture
    {
        private const float MaxDuration = 1.65f;
        private const float MaxPause = 0.23f;
        private const float MinimumTravel = 75f;

        private Vector2 previous;
        private Vector2 displacement;
        private float travel;
        private float turn;
        private float absoluteTurn;
        private float startedAt;
        private float lastMotion;

        public bool Sample(Vector2 delta, float now)
        {
            if (travel > 0f && (now - startedAt > MaxDuration || now - lastMotion > MaxPause))
                Reset();

            var distance = delta.magnitude;
            if (distance < 0.5f) return false;

            if (travel == 0f)
                startedAt = now;
            else
            {
                var angle = Vector2.SignedAngle(previous, delta);
                turn += angle;
                absoluteTurn += Mathf.Abs(angle);
            }

            previous = delta;
            lastMotion = now;
            displacement += delta;
            travel += distance;

            if (travel < MinimumTravel || Mathf.Abs(turn) < 300f ||
                Mathf.Abs(turn) < absoluteTurn * 0.8f || displacement.magnitude > travel * 0.16f)
                return false;

            Reset();
            return true;
        }

        public void Reset()
        {
            previous = displacement = Vector2.zero;
            travel = turn = absoluteTurn = 0f;
        }
    }
}
