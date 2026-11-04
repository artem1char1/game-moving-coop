using UnityEngine;
using UnityEngine.InputSystem;

namespace MovingCoop
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Transform view;
        [SerializeField] private Transform modelRoot;
        [SerializeField, Min(0f)] private float speed = 4f;
        [SerializeField, Min(0f)] private float turnSpeed = 540f;

        private CharacterController controller;

        private void Awake() => controller = GetComponent<CharacterController>();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!Application.isFocused || keyboard == null) return;

            var input = new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));

            Move(input, Time.deltaTime);
        }

        private void Move(Vector2 input, float deltaTime)
        {
            if (input == Vector2.zero) return;

            input = Vector2.ClampMagnitude(input, 1f);
            var right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            var forward = Vector3.Cross(right, Vector3.up);
            var direction = right * input.x + forward * input.y;

            controller.Move(direction * (speed * deltaTime));
            var targetRotation = Quaternion.LookRotation(direction);
            modelRoot.rotation = Quaternion.RotateTowards(modelRoot.rotation, targetRotation, turnSpeed * deltaTime);
        }
    }
}
