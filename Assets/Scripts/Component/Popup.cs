using Unity.Entities;

[GenerateAuthoringComponent]
public struct Popup : IComponentData
{
    public enum Subject { win, lose };

    public Subject subject;
}
