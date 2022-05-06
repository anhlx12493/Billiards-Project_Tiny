using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct Cue : IComponentData
{
    public float3 posAim;
    public bool isMine;
}
