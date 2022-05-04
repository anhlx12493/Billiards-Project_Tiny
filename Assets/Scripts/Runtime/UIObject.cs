using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct UIObject : IComponentData
{
    public enum Alignment { middle, top, left, right, bottom }
    public float3 size;
    public Alignment alignment;
    public float alignValue;
}
