using Unity.Entities;

[GenerateAuthoringComponent]
public struct GameManager : IComponentData
{
    public Entity popupWin;
    public Entity popupLose;
    public Entity targetInfoBall0;
    public Entity targetInfoBall1;
}
