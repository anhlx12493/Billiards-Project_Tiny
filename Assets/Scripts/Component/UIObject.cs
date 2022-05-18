using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct UIObject : IComponentData
{
    public enum AlignmentHorizontal {none, left, right}
    public enum AlignmentVertical { none, top, bottom}
    public float3 size;
    public AlignmentHorizontal alignmentHorizontal;
    public float alignHorizontalValue;
    public AlignmentVertical alignmentVertical;
    public float alignVerticalValue;
}
