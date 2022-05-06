using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using FixMath.NET;
using UnityEngine;

namespace Billiards
{
    public class PhysicsShapeBox : PhysicsShape
    {
        public Vector3 size = Vector3.one;

        public override EntityShape CreateShape()
        {
            var tScale = transform.localScale;
            var width = (Fix64)(size.x * tScale.x);
            var height = (Fix64)(size.y * tScale.y);
            var length = (Fix64)(size.z * tScale.z);
            return new BoxShape(width, height, length);
        }
    }
}