using Unity.Entities;
using Unity.Tiny.Rendering;
using UnityEngine;

[GenerateAuthoringComponent]
public struct Glow : IComponentData
{
    public int serialBall;
    public int serial;
}
