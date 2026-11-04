using UnityEngine;
using UnityEngine.InputSystem;

namespace MovingCoop
{
    internal enum JumpGesture { Circle, Horizontal, Vertical }

    internal readonly struct AttemptControls
    {
        private static readonly Key[] MovementKeys =
        {
            Key.Q, Key.E, Key.T, Key.Y, Key.U, Key.I, Key.O, Key.P,
            Key.F, Key.G, Key.H, Key.J, Key.K, Key.L, Key.Z, Key.X,
            Key.C, Key.V, Key.B, Key.N, Key.M
        };

        private static AttemptControls? previous;

        public readonly Key Forward;
        public readonly Key Back;
        public readonly Key Left;
        public readonly Key Right;
        public readonly JumpGesture Jump;

        private AttemptControls(Key forward, Key back, Key left, Key right, JumpGesture jump)
        {
            Forward = forward;
            Back = back;
            Left = left;
            Right = right;
            Jump = jump;
        }

        public static AttemptControls Next()
        {
            var keys = (Key[])MovementKeys.Clone();
            AttemptControls next;
            do
            {
                for (var i = 0; i < 4; i++)
                {
                    var pick = Random.Range(i, keys.Length);
                    (keys[i], keys[pick]) = (keys[pick], keys[i]);
                }

                var jump = previous.HasValue
                    ? (JumpGesture)(((int)previous.Value.Jump + Random.Range(1, 3)) % 3)
                    : (JumpGesture)Random.Range(0, 3);
                next = new AttemptControls(keys[0], keys[1], keys[2], keys[3], jump);
            } while (previous.HasValue && next.SameMovement(previous.Value));

            previous = next;
            return next;
        }

        private bool SameMovement(AttemptControls other) =>
            Forward == other.Forward && Back == other.Back && Left == other.Left && Right == other.Right;

        public string Describe()
        {
            var jump = Jump switch
            {
                JumpGesture.Horizontal => "Мышь влево-вправо — прыжок",
                JumpGesture.Vertical => "Мышь вверх-вниз — прыжок",
                _ => "Круг курсором — прыжок"
            };
            return $"{Forward} — вперёд   {Back} — назад\n{Left} — влево   {Right} — вправо\n{jump}\nR — начать заново";
        }
    }
}
