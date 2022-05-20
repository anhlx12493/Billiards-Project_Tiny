using Unity.Entities;
using Unity.Tiny;

namespace Billiards
{
    [GenerateAuthoringComponent]
    public struct UIButton : IComponentData
    {
        public enum Subject { Continue, Replay };
        public Subject subject;
        public Rect clickFeild;
    }
}
