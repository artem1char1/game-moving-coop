using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MovingCoop
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField] private Transform modelRoot;
        [SerializeField, Min(0f)] private float speed = 4f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;

        private const float Gravity = 22f;
        private readonly MouseJumpGesture jumpGesture = new();
        private CharacterController controller;
        private float verticalSpeed;

        internal AttemptControls Controls { get; private set; }

        private void Awake()
        {
            Controls = AttemptControls.Next();
            controller = GetComponent<CharacterController>();
            if (view == null && Camera.main != null)
                view = Camera.main.transform;
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
                    jump = jumpGesture.Sample(mouse.position.ReadValue(), Time.unscaledTime, Controls.Jump);
                else
                    jumpGesture.Reset();
            }
            else
            {
                jumpGesture.Reset();
            }

            Move(input, jump, Time.deltaTime);
        }

        private static int Pressed(KeyControl key) => key.isPressed ? 1 : 0;

        private void Move(Vector2 input, bool jump, float deltaTime)
        {
            input = Vector2.ClampMagnitude(input, 1f);
            var right = view != null ? Vector3.ProjectOnPlane(view.right, Vector3.up).normalized : Vector3.right;
            var forward = Vector3.Cross(right, Vector3.up);
            var direction = right * input.x + forward * input.y;

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
