using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct Cue : IComponentData
{
    public bool isMine;
    public bool isSlide;
}
