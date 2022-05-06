using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using FixMath.NET;

namespace Billiards
{
    public class PhysicsShapeSphere : PhysicsShape
    {
        public float radius = 0.5f;

        public override EntityShape CreateShape()
        {
            var tScale = transform.localScale;
            float maxScale = tScale.x;
            maxScale = maxScale < tScale.y ? tScale.y : maxScale;
            maxScale = maxScale < tScale.z ? tScale.z : maxScale;
            return new SphereShape((Fix64)(radius * maxScale));
        }
    }
}