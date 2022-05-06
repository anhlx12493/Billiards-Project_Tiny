using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using FixMath.NET;
using UnityEngine;
using BMaterial = BEPUphysics.Materials.Material;

namespace Billiards
{
    public delegate void OnBodyAdded(int idx, BodyDescription desc);
    public delegate void OnKinematicAdded(int idx, KinematicDescription desc);

    public abstract class PhysicsShape : MonoBehaviour
    {
        public static OnBodyAdded OnBodyAdded = null;
        public static OnKinematicAdded OnKinematicAdded = null;

        public abstract EntityShape CreateShape();

        public Vector3 center;
        public PhysicMaterial material;
        public bool isTrigger;
        public int index = -1;

        private void Start()
        {
            var shape = CreateShape();
            BMaterial mat = null;
            var pos = PhysicsHelper.ToBVector3(transform.position + center);
            var rot = PhysicsHelper.ToBQuaternion(transform.rotation);
            var pb = GetComponent<PhysicsBody>();

            if (material != null)
            {
                mat = new BMaterial
                {
                    KineticFriction = (Fix64)material.dynamicFriction,
                    StaticFriction = (Fix64)material.staticFriction,
                    Bounciness = (Fix64)material.bounciness
                };
            }

            if (pb != null)
            {
                var ballRadius = ((SphereShape)shape).Radius;
                var mass = (Fix64)pb.mass;
                var diagonalInertia = 0.4m * mass * ballRadius * ballRadius;
                var ballLocalInertiaTensor = new BEPUutilities.Matrix3x3(
                    diagonalInertia, 0, 0, 0, diagonalInertia, 0, 0, 0, diagonalInertia);

                OnBodyAdded?.Invoke(index, new BodyDescription
                {
                    Position = pos,
                    Orientation = rot,
                    Mass = mass,
                    LocalInertia = ballLocalInertiaTensor,
                    Shape = shape,
                    Material = mat,
                    Tag = transform
                });
            }
            else
            {
                OnKinematicAdded?.Invoke(index, new KinematicDescription
                {
                    Position = pos,
                    Orientation = rot,
                    Shape = shape,
                    Material = mat,
                    IsTrigger = isTrigger,
                    Tag = transform
                });
            }
        }
    }
}