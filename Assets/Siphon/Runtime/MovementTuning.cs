using UnityEngine;

namespace Siphon
{
    [CreateAssetMenu(menuName = "Siphon/Movement tuning")]
    public sealed class MovementTuning : ScriptableObject
    {
        public float groundSpeed = 16f;
        public float groundAcceleration = 90f;
        public float airAcceleration = 12f;
        public float skiAcceleration = 14f;
        public float groundFriction = 9f;
        public float gravity = 26f;
        public float jumpSpeed = 12f;
        public float jetAcceleration = 38f;
        public float jetCapacity = 2.5f;
        public float jetRecharge = 0.65f;
        public float grappleRange = 150f;
        public float grappleAcceleration = 45f;
        public float maximumSpeed = 85f;
        public float mouseSensitivity = 2f;
    }
}
