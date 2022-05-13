using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct Pocket : IComponentData
{
    public float3 directionGoInsideBoard;
}
