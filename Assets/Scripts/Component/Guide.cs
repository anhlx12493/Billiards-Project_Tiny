using Unity.Entities;

[GenerateAuthoringComponent]
public struct Guide : IComponentData
{
    public enum ID { hitPoint, LineCueIncidence, LineCueReflection, LineBeHit};

    public ID id;
}
