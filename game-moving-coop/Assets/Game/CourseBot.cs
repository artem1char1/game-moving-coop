using UnityEngine;

namespace MovingCoop
{
    internal sealed class CourseBot
    {
        internal readonly struct Waypoint
        {
            public readonly Vector2 Position;
            public readonly bool Jump;

            public Waypoint(float x, float z, bool jump = false)
            {
                Position = new Vector2(x, z);
                Jump = jump;
            }
        }

        private readonly CharacterController controller;
        private readonly Waypoint[] route;
        private readonly float speed;
        private int waypoint;
        private float verticalSpeed;
        private bool jumped;

        public float Z => controller.transform.position.z;

        public CourseBot(Transform parent, string name, Vector3 spawn, Color color, float speed,
            Waypoint[] route, CharacterController player, CourseBot[] otherBots)
        {
            this.route = route;
            this.speed = speed;

            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = spawn;

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(root.transform, false);
            capsule.transform.localPosition = Vector3.up * .75f;
            capsule.transform.localScale = new Vector3(.5f, .75f, .5f);
            var primitiveCollider = capsule.GetComponent<Collider>();
            primitiveCollider.enabled = false;
            Object.Destroy(primitiveCollider);
            var tint = new MaterialPropertyBlock();
            tint.SetColor("_BaseColor", color);
            capsule.GetComponent<Renderer>().SetPropertyBlock(tint);

            controller = root.AddComponent<CharacterController>();
            controller.center = Vector3.up * .75f;
            controller.radius = .25f;
            controller.height = 1.5f;
            controller.minMoveDistance = 0f;
            Physics.IgnoreCollision(controller, player);
            foreach (var bot in otherBots)
                if (bot != null) Physics.IgnoreCollision(controller, bot.controller);
        }

        public void Step(float deltaTime)
        {
            if (waypoint >= route.Length) return;

            var position = controller.transform.position;
            var target = route[waypoint];
            var offset = target.Position - new Vector2(position.x, position.z);
            if (offset.sqrMagnitude <= .04f * .04f)
            {
                waypoint++;
                jumped = false;
                return;
            }

            const float gravity = 22f;
            if (controller.isGrounded && verticalSpeed <= 0f)
            {
                var jump = target.Jump && !jumped;
                verticalSpeed = jump ? Mathf.Sqrt(2f * gravity * 1.2f) : -2f;
                jumped |= jump;
            }

            verticalSpeed -= gravity * deltaTime;
            var step = Vector2.ClampMagnitude(offset, speed * deltaTime);
            var collision = controller.Move(new Vector3(step.x, verticalSpeed * deltaTime, step.y));
            if ((collision & CollisionFlags.Above) != 0) verticalSpeed = 0f;
        }
    }
}
