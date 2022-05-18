using Unity.Entities;
using Unity.Mathematics;
using Unity.Tiny;
using Unity.Tiny.Input;
using Unity.Tiny.UI;
using Unity.Transforms;
using Unity.Tiny.Rendering;

namespace Billiards
{
    public class UIController : SystemBase
    {
        private const double HEIGHT_SIZE_CAMERA = 7.24f;
        private const double HAFT_HEIGHT_SIZE_CAMERA = 3.62f;
        private const double POSITION_Y_CAMERA = 0.747f;

        public static float3 WorldMousePosition { get; private set; }

        private InputSystem Input;
        private double screenWidth, screenHeight, lastScreenWidth, lastScreenHeight;

        private int isStartRunning = 3;

        private float currentHaftHeightSizeCamera = (float)HAFT_HEIGHT_SIZE_CAMERA;
        private float currentHeightSizeCamera = (float)HEIGHT_SIZE_CAMERA;


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
            UpdateCamera();
            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;
        }

        private void UpdateCamera()
        {
            if (lastScreenWidth != screenWidth || lastScreenHeight != screenHeight)
            {
                if (screenHeight / screenWidth > 0.5625f)
                {
                    Entities.ForEach((ref Camera camera) =>
                    {
                        currentHaftHeightSizeCamera = (float)(HAFT_HEIGHT_SIZE_CAMERA * (screenHeight / screenWidth / 0.5625f));
                        camera.fov = currentHaftHeightSizeCamera;
                        currentHeightSizeCamera = currentHaftHeightSizeCamera * 2;
                    }).WithoutBurst().Run();
                }
                else
                {
                    Entities.ForEach((ref Camera camera) =>
                    {
                        currentHaftHeightSizeCamera = (float)(HAFT_HEIGHT_SIZE_CAMERA);
                        camera.fov = currentHaftHeightSizeCamera;
                        currentHeightSizeCamera = currentHaftHeightSizeCamera * 2;
                    }).WithoutBurst().Run();
                }
            }
        }

        private void UpdateWorldMousePosition()
        {
            var posClick = Input.GetInputPosition();
            double x = currentHeightSizeCamera * (screenWidth / screenHeight);
            float3 worldClick = float3.zero;
            worldClick.x = (float)(posClick.x / screenWidth * x - x / 2d);
            worldClick.z = (float)(posClick.y / screenHeight * currentHeightSizeCamera - currentHaftHeightSizeCamera + POSITION_Y_CAMERA);
            WorldMousePosition = worldClick;
        }

        private void Alignmemt()
        {
            Entities.ForEach((ref UIObject uIObject, ref Translation position) =>
            {
                switch (uIObject.alignmentHorizontal)
                {
                    case UIObject.AlignmentHorizontal.left:
                        position.Value.x = (float)(-screenWidth / screenHeight * currentHaftHeightSizeCamera + uIObject.alignHorizontalValue);
                        break;
                    case UIObject.AlignmentHorizontal.right:
                        position.Value.x = (float)(screenWidth / screenHeight * currentHaftHeightSizeCamera - uIObject.alignHorizontalValue);
                        break;
                }
                switch (uIObject.alignmentVertical)
                {
                    case UIObject.AlignmentVertical.top:
                        position.Value.z = (float)(currentHaftHeightSizeCamera + POSITION_Y_CAMERA - uIObject.alignVerticalValue);
                        break;
                    case UIObject.AlignmentVertical.bottom:
                        position.Value.z = (float)(-currentHaftHeightSizeCamera + POSITION_Y_CAMERA + uIObject.alignVerticalValue);
                        break;
                }
            }).WithoutBurst().Run();
        }
    }
}
