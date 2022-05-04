using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct PhysicsSetting : IComponentData
{
    public float friction;
    public float frictionSurface;
    public float radiusBall;
}
