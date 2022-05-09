using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Tiny;
using Unity.Tiny.Input;
using Unity.Tiny.UI;
using Unity.Transforms;

namespace Billiards {
    [UpdateAfter(typeof(Physics))]
    public class BallController : SystemBase
    {

        private float3[] positionsBall = new float3[16];

        protected override void OnCreate()
        {
        }

        protected override void OnUpdate()
        {
            var Input = World.GetExistingSystem<InputSystem>();
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var canvas = EntityManager.GetComponentData<RectTransform>(GetSingletonEntity<MainCanvas>());
                var posClick = Input.GetInputPosition();
                float x = 10 * (canvas.SizeDelta.x / canvas.SizeDelta.y);
                float3 worldClick = float3.zero;
                worldClick.x = (posClick.x / canvas.SizeDelta.x) * x - x / 2f;
                worldClick.z = (posClick.y / canvas.SizeDelta.y) * 10 - 5;
                Hit((worldClick - GetPositionCueBall()) * 0.015f);
            }
            UpdateAttach();
        }

        private float3 GetPositionCueBall()
        {
            float3 result = float3.zero;
            bool isCue = true;
            Entities.ForEach((ref Ball ball, ref Translation pos) => {
                if (isCue)
                {
                    isCue = false;
                    result = pos.Value;
                }
            }).Run();
            return result;
        }

        private void Hit(float3 force)
        {
            force.y = 0;
            bool isCue = true;
            Entities.ForEach((ref Ball ball) => {
                if (isCue)
                {
                    isCue = false;
                    ball.addForce = force;
                }
            }).Run();
        }

        private void UpdateAttach()
        {
            int i = 0;
            Entities.ForEach((ref Ball ball, ref Translation pos) => {
                positionsBall[i++] = pos.Value;
            }).WithoutBurst().Run();
            i = 0;
            Entities.ForEach((ref Attach attach, ref Translation pos) => {
                pos.Value = positionsBall[i++];
            }).WithoutBurst().Run();
        }
    } }
