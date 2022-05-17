using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct UIObject : IComponentData
{
    public enum Alignment { none ,middle, top, left, right}
    public float3 size;
    public Alignment alignment, alignmentExtra;
    public float alignValue, alignValueExtra;
}
