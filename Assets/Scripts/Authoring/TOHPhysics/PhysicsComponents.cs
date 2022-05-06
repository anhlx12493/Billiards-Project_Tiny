using BEPUphysics.CollisionShapes;
using BEPUphysics.Materials;
using BEPUutilities;
using FixMath.NET;
using System;

namespace Billiards
{
    public struct BodyDescription
    {
        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public Fix64 Mass;
        public Matrix3x3 LocalInertia;
        public EntityShape Shape;
        public Material Material;
        public object Tag;
    }
    public struct CueShot : IEquatable<CueShot>
    {
        public int BodyIndex;
        public Vector3 Offset;
        public Vector3 Normal;
        public Vector3 ImpactDirection;
        public Fix64 ImpactVelocity;
        // Coefficient of friction to use for the cue-ball interaction.
        // Depending on chalk and material, values around 0.6 to 0.7 are reasonable.
        public Fix64 CoefficientOfFriction;
        // Coefficient of restitution to use for the cue-ball interaction.
        // Leather cue tips are around 0.7 to 0.75, while harder phenolic tips might range from 0.8 to 0.87.
        public Fix64 CoefficientOfRestitution;
        public Fix64 AngularScale;

        public void Reset()
        {
            BodyIndex = -1;
            Offset = Normal = ImpactDirection = Vector3.Zero;
            ImpactVelocity = CoefficientOfFriction = CoefficientOfRestitution = AngularScale = 0;
        }

        public bool Equals(CueShot other)
        {
            return
                BodyIndex == other.BodyIndex &&
                Offset == other.Offset &&
                Normal == other.Normal &&
                ImpactDirection == other.ImpactDirection &&
                ImpactVelocity == other.ImpactVelocity &&
                CoefficientOfFriction == other.CoefficientOfFriction &&
                CoefficientOfRestitution == other.CoefficientOfRestitution &&
                AngularScale == other.AngularScale;
        }

        public override string ToString()
        {
            return string.Format("BodyIndex = {0}\n" +
                "Offset = {1}\n" +
                "Normal = {2}\n" +
                "ImpactDirection = {3}\n" +
                "ImpactVelocity = {4}\n" +
                "CoefficientOfFriction = {5}\n" +
                "CoefficientOfRestitution = {6}\n" +
                "AngularScale = {7}",
                BodyIndex, Offset, Normal, ImpactDirection,
                ImpactVelocity, CoefficientOfFriction, CoefficientOfRestitution, AngularScale);
        }
    }
    public struct SimulationDescription
    {
        public Vector3 Gravity;
        public Fix64 TimeStepDuration;
        public Fix64 VelocityLowerLimit;
        public BodyDescription[] Bodies;
        public KinematicDescription[] Kinematics;
        public MeshDescription[] Meshes;

        public static SimulationDescription CreateCopy(SimulationDescription desc)
        {
            var copy = desc;
            copy.Bodies = new BodyDescription[desc.Bodies.Length];
            copy.Kinematics = new KinematicDescription[desc.Kinematics.Length];
            copy.Meshes = new MeshDescription[desc.Meshes.Length];
            desc.Bodies.CopyTo(copy.Bodies, 0);
            desc.Kinematics.CopyTo(copy.Kinematics, 0);
            desc.Meshes.CopyTo(copy.Meshes, 0);
            return copy;
        }
    }
    public struct KinematicDescription
    {
        public Vector3 Position;
        public Quaternion Orientation;
        public EntityShape Shape;
        public Material Material;
        public bool IsTrigger;
        public object Tag;
    }

    public struct MeshDescription
    {
        public AffineTransform Transform;
        public InstancedMeshShape Shape;
        public Material Material;
        public object Tag;
    }
    public struct BodyPose
    {
        public Vector3 Position;
        public Quaternion Orientation;
    }
}