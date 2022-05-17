using Unity.Entities;
using Unity.Mathematics;
using Unity.Tiny;
using Unity.Tiny.Input;
using Unity.Tiny.UI;
using Unity.Transforms;

namespace Billiards
{
    public class UIController : SystemBase
    {
        private const double HEIGHT_SIZE_CAMERA = 7.24f;
        private const double HAFT_HEIGHT_SIZE_CAMERA = 3.62f;
        private const double POSITION_Y_CAMERA = 0.747f;

        public static float3 WorldMousePosition { get; private set; }

        private InputSystem Input;
        private double screenWidth, screenHeight;

        private int isStartRunning = 3;

        protected override void OnStartRunning()
        {
        }
        protected override void OnUpdate()
        {
            Input = World.GetExistingSystem<InputSystem>();
            var canvas = EntityManager.GetComponentData<RectTransform>(GetSingletonEntity<MainCanvas>());
            screenWidth = canvas.SizeDelta.x;
            screenHeight = canvas.SizeDelta.y;
            UpdateWorldMousePosition();
            Alignmemt();
        }

        private void UpdateWorldMousePosition()
        {
            var posClick = Input.GetInputPosition();
            double x = HEIGHT_SIZE_CAMERA * (screenWidth / screenHeight);
            float3 worldClick = float3.zero;
            worldClick.x = (float)(posClick.x / screenWidth * x - x / 2d);
            worldClick.z = (float)(posClick.y / screenHeight * HEIGHT_SIZE_CAMERA - HAFT_HEIGHT_SIZE_CAMERA + POSITION_Y_CAMERA);
            WorldMousePosition = worldClick;
        }

        private void Alignmemt()
        {
            Entities.ForEach((ref UIObject uIObject, ref Translation position) =>
            {
                switch (uIObject.alignment)
                {
                    case UIObject.Alignment.left:
                        position.Value.x = (float)(-screenWidth / screenHeight * HAFT_HEIGHT_SIZE_CAMERA + uIObject.alignValue);
                        break;
                    case UIObject.Alignment.right:
                        position.Value.x = (float)(screenWidth / screenHeight * HAFT_HEIGHT_SIZE_CAMERA - uIObject.alignValue);
                        break;
                    case UIObject.Alignment.top:
                        position.Value.z = (float)(HAFT_HEIGHT_SIZE_CAMERA - uIObject.alignValue);
                        break;
                }
                switch (uIObject.alignmentExtra)
                {
                    case UIObject.Alignment.left:
                        position.Value.x = (float)(-screenWidth / screenHeight * HAFT_HEIGHT_SIZE_CAMERA + uIObject.alignValueExtra);
                        break;
                    case UIObject.Alignment.right:
                        position.Value.x = (float)(screenWidth / screenHeight * HAFT_HEIGHT_SIZE_CAMERA - uIObject.alignValueExtra);
                        break;
                    case UIObject.Alignment.top:
                        position.Value.z = (float)(HAFT_HEIGHT_SIZE_CAMERA - uIObject.alignValueExtra);
                        break;
                }
            }).WithoutBurst().Run();
        }
    }
}
