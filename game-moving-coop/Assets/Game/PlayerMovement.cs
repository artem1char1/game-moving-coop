using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MovingCoop
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField, Min(0f)] private float speed = 4f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;

        private const float Gravity = 22f;
        private const float ScrollWindow = .3f;
        private readonly MouseJumpGesture jumpGesture = new();
        private CharacterController controller;
        private float verticalSpeed;
        private float scrollUntil = float.NegativeInfinity;
        private bool scrollUp;

        internal AttemptControls Controls { get; private set; }

        private void Awake()
        {
            Controls = AttemptControls.Next();
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            var input = Vector2.zero;
            var jump = false;
            if (Application.isFocused)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    input = new Vector2(
                        Pressed(keyboard[Controls.Right]) - Pressed(keyboard[Controls.Left]),
                        Pressed(keyboard[Controls.Forward]) - Pressed(keyboard[Controls.Back]));
                }

                var mouse = Mouse.current;
                if (mouse != null)
                    input = FilterMovement(input, mouse.scroll.ReadValue().y, Time.unscaledTime);
                else
                {
                    input = Vector2.zero;
                    scrollUntil = float.NegativeInfinity;
                }

                if (mouse != null && !mouse.rightButton.isPressed && !mouse.rightButton.wasReleasedThisFrame)
                    jump = jumpGesture.Sample(mouse.position.ReadValue(), Time.unscaledTime, Controls.Jump);
                else
                    jumpGesture.Reset();
            }
            else
            {
                scrollUntil = float.NegativeInfinity;
                jumpGesture.Reset();
            }

            Move(input, jump, Time.deltaTime);
        }

        private Vector2 FilterMovement(Vector2 input, float scroll, float now)
        {
            if (scroll != 0f)
            {
                scrollUp = scroll > 0f;
                scrollUntil = now + ScrollWindow;
            }

            if (now >= scrollUntil) return Vector2.zero;
            return scrollUp ? new Vector2(0f, input.y) : new Vector2(input.x, 0f);
        }

        private static int Pressed(KeyControl key) => key.isPressed ? 1 : 0;

        private void Move(Vector2 input, bool jump, float deltaTime)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            var direction = new Vector3(input.x, 0f, input.y);

            if (controller.isGrounded && verticalSpeed <= 0f)
                verticalSpeed = jump ? Mathf.Sqrt(2f * Gravity * jumpHeight) : -2f;

            verticalSpeed -= Gravity * deltaTime;
            var collisions = controller.Move((direction * speed + Vector3.up * verticalSpeed) * deltaTime);
            if ((collisions & CollisionFlags.Above) != 0)
                verticalSpeed = 0f;

            if (direction != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(direction);
                modelRoot.rotation = Quaternion.RotateTowards(modelRoot.rotation, targetRotation, turnSpeed * deltaTime);
            }
        }
    }
}
