using Unity.Entities;

[GenerateAuthoringComponent]
public struct Guide : IComponentData
{
    public enum ID { hitPoint, hitWrongPoint, LineCueIncidence, LineCueReflection, LineBeHit};

    public ID id;
}
