using Unity.Entities;

[GenerateAuthoringComponent]
public struct TimeDown : IComponentData
{
    public enum Subject { you, bot };

    public Subject subject;
    public float size;
}
