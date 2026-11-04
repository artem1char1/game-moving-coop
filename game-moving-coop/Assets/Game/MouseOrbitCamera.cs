using UnityEngine;
using UnityEngine.InputSystem;

namespace MovingCoop
{
    public sealed class MouseOrbitCamera : MonoBehaviour
    {
        private static readonly Vector3 Pivot = Vector3.up * .75f;
        private const float Sensitivity = .22f;
        private float distance;
        private float pitch;
        private float yaw;

        private void Awake()
        {
            distance = Vector3.Distance(transform.localPosition, Pivot);
            var angles = transform.localEulerAngles;
            pitch = Mathf.DeltaAngle(0f, angles.x);
            yaw = angles.y;
        }

        private void LateUpdate()
        {
            var mouse = Mouse.current;
            if (!Application.isFocused || mouse == null || !mouse.rightButton.isPressed ||
                mouse.rightButton.wasPressedThisFrame)
                return;

            var delta = mouse.delta.ReadValue();
            if (delta != Vector2.zero)
                Orbit(delta);
        }

        private void Orbit(Vector2 delta)
        {
            yaw = Mathf.Repeat(yaw + delta.x * Sensitivity, 360f);
            pitch = Mathf.Clamp(pitch - delta.y * Sensitivity, 36f, 72f);
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetLocalPositionAndRotation(Pivot - rotation * Vector3.forward * distance, rotation);
        }
    }
}
