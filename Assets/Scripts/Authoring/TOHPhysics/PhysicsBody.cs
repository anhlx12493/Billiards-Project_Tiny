using UnityEngine;

namespace Billiards
{
    public class PhysicsBody : MonoBehaviour
    {
        public float mass;
        [Range(0, 1)]
        public float damping;
        [Range(0, 1)]
        public float angularDamping;
    }
}