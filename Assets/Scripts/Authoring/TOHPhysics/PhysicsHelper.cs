using FixMath.NET;
using UnityEngine;
using BVector3 = BEPUutilities.Vector3;
using BQuaternion = BEPUutilities.Quaternion;
using BMaterial = BEPUphysics.Materials.Material;
using System;

public static class PhysicsHelper
{
    public static BVector3 ToBVector3(Vector3 v)
    {
        return new BVector3((Fix64)v.x, (Fix64)v.y, (Fix64)v.z);
    }

    public static Vector3 FromBVector3(BVector3 v)
    {
        return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
    }

    public static BQuaternion ToBQuaternion(Quaternion q)
    {
        return new BQuaternion((Fix64)q.x, (Fix64)q.y, (Fix64)q.z, (Fix64)q.w);
    }

    public static Quaternion FromBQuaternion(BQuaternion q)
    {
        return new Quaternion((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);
    }

    public static BMaterial ToBMaterial(PhysicMaterial m)
    {
        return new BMaterial((Fix64)m.staticFriction, (Fix64)m.dynamicFriction,
            (Fix64)m.bounciness);
    }

    public static BVector3 Truncate(BVector3 v, int digit = 2)
    {
        return new BVector3(TruncateFix(v.X, digit), TruncateFix(v.Y, digit),
            TruncateFix(v.Z, digit));
    }

    public static Fix64 TruncateFix(Fix64 n, int digit = 2)
    {
        int i = (int)Fix64.Floor(n);
        float f = (float)Fix64.FractionalPart(n);
        f = (float)Math.Round(f, digit);
        return (Fix64)(i + f);
    }
}
