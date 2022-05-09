using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System;
using Unity.Tiny.Input;
using Unity.Tiny.UI;
using Unity.Tiny;

namespace Billiards
{
    public class GameController : SystemBase
    {

        private float radius = 0.1024f;

        protected override void OnUpdate()
        {
            var Input = World.GetExistingSystem<InputSystem>();

            var posCueBall = GetPositionCueBall();
            var canvas = EntityManager.GetComponentData<RectTransform>(GetSingletonEntity<MainCanvas>());
            var posClick = Input.GetInputPosition();
            float x = 10 * (canvas.SizeDelta.x / canvas.SizeDelta.y);
            float3 worldClick = float3.zero;
            worldClick.x = (posClick.x / canvas.SizeDelta.x) * x - x / 2f;
            worldClick.z = (posClick.y / canvas.SizeDelta.y) * 10 - 5;
            ShowGuide((worldClick - posCueBall));
        }

        private void FixRadius()
        {

        }

        private float3 GetPositionCueBall()
        {
            float3 result = float3.zero;
            bool isCue = true;
            Entities.ForEach((ref Ball ball, ref Translation position) =>
            {
                if (isCue)
                {
                    isCue = false;
                    result = position.Value;
                }
            }).Run();
            return result;
        }

        private void ShowCue(bool isMyTurn)
        {
            Entities.ForEach((ref Cue cue) =>
            {
                if (cue.isMine == isMyTurn)
                {
                }
            }).WithoutBurst().Run();
        }

        private void HideCue()
        {

        }

        private void ShowGuide(float3 force)
        {
            force.y = 0;
            float3 posCueBall = float3.zero;
            float3 posTargetBall = float3.zero;
            float3 hitPosition = float3.zero;
            bool isHit = false;

            posCueBall = GetPositionCueBall();

            int i = 0;
            Entities.ForEach((ref Ball ball, ref Translation position) =>
            {
                if (i == 1)
                {
                    posTargetBall = position.Value;
                    GetHitPositionBallToBall(posCueBall, posTargetBall, force, out isHit, out hitPosition);
                }
                i++;
            }).WithoutBurst().Run();
            if (isHit)
            {
                Entities.ForEach((ref Guide guide, ref Translation position, ref Rotation rotation, ref NonUniformScale scale) =>
                {
                    switch (guide.id)
                    {
                        case Guide.ID.hitPoint:
                            position.Value = hitPosition;
                            break;
                        case Guide.ID.LineCueIncidence:
                            DrawLine(ref position, ref rotation, ref scale, posCueBall, hitPosition);
                            break;
                        case Guide.ID.LineCueReflection:
                            break;
                        case Guide.ID.LineBeHit:
                            float3 reflectForce = posTargetBall - hitPosition;
                            float3 reflectPosition = posTargetBall + reflectForce * 10;

                            DrawLine(ref position, ref rotation, ref scale, posTargetBall, reflectPosition);
                            break;
                    }
                    position.Value.y = 1;
                }).WithoutBurst().Run();
            }
            else
            {
                Entities.ForEach((ref Guide guide, ref Translation position) =>
                {
                    position.Value = new float3(1000, position.Value.y, 1000);
                }).WithoutBurst().Run();
            }
        }

        private void HideGuide()
        {

        }

        private void DrawLine(ref Translation position, ref Rotation rotation, ref NonUniformScale scale, float3 pos1, float3 pos2)
        {
            position.Value = pos1 + (pos2 - pos1) / 2;
            scale.Value.x = (float)Math.Sqrt(Math.Pow(pos2.x - pos1.x, 2) + Math.Pow(pos2.z - pos1.z, 2)) / 10f;
            float3 force = pos2 - pos1;
            rotation.Value = quaternion.LookRotation(new float3(-force.z, 0, force.x), new float3(0, 1, 0));
        }

        private void QuadraticEquation2(float a, float b, float c, out float x1, out float x2)
        {
            float del = b * b - 4 * a * c;
            if (del >= 0)
            {
                del = (float)Math.Sqrt(del);
                x1 = (-b + del) / (2 * a);
                x2 = (-b - del) / (2 * a);
            }
            else
            {
                x1 = x2 = float.NaN;
            }

        }

        private void GetHitPositionBallToBall(float3 posStart, float3 posTarget, float3 force, out bool isHit, out float3 result)
        {
            float a = force.z;
            float b = -force.x;
            float c = -a * posStart.x - b * posStart.z;
            float i1 = posTarget.x;
            float i2 = posTarget.z;
            float p1 = a / b;
            float p2 = c / b;
            float A = 1 + p1 * p1;
            float B = 2 * (a * c / (b * b) - i1 + i2 * a / b);
            float C = p2 * p2 + 2 * i2 * c / b + i1 * i1 + i2 * i2 - Physics.powDiameterBall;
            float x1, x2;
            QuadraticEquation2(A, B, C, out x1, out x2);
            isHit = false;
            if (float.IsNaN(x1))
            {
                result = float3.zero;
            }
            else
            {
                if (math.abs(x1 - posStart.x) > math.abs(x2 - posStart.x))
                {
                    x1 = x2;
                }
                float y = (-a * x1 - c) / b;
                result = new float3(x1, 0, y);
                float3 nextPos = posStart + force;
                if (posStart.x > result.x && posStart.x > nextPos.x || posStart.x < result.x && posStart.x < nextPos.x)
                {
                    if (posStart.z > result.z && posStart.z > nextPos.z || posStart.z < result.z && posStart.z < nextPos.z)
                    {
                        isHit = true;
                    }
                }
                if (!isHit)
                {
                    result = float3.zero;
                }
            }
        }
    }
}
