using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[GenerateAuthoringComponent]
public struct Ball : IComponentData
{
    public byte id;
    public Translation position;

    public float3 addForce;
}
