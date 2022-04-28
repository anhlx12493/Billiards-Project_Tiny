using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Tiny;
using Unity.Transforms;

namespace Billiards
{
    [UpdateAfter(typeof(GameController))]
    public class BallController : SystemBase
    {
        public const float MIN_POSITION_TRACK_X = -2.4f;

        public static BallController Instance;

        private float3[] positionsBall = new float3[16];
        private float3[] velocitysBall = new float3[16];
        private byte[] trackCountBall = new byte[16];
        public bool[] isPocketBall = new bool[16];

        private float3 positionTrack;
        private byte trackCount;

        public bool firstHit = true;
        public bool isBreakShot = true;
        public int serialHitBall = -1;
        public bool hit;
        public bool isBallStillRollingOnPack;
        public float3 positionFirstHit;

        protected override void OnCreate() 
        {
            Instance = this;
        }

        protected override void OnUpdate()
        {
            GetData();
            ForceDownBall();
            CheckFirstHit();
            UpdateAttach();
            HandleBreakShot();
            HandleCheckTrack();
        }

        private void GetData()
        {
            Entities.ForEach((ref TrackPosition track, ref Translation position) =>
            {
                positionTrack = position.Value;
            }).WithoutBurst().Run();
        }

        private void ForceDownBall()
        {
            Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation positon) =>
            {
                if (positon.Value.y >=0)
                {
                    positon.Value.y = 0;
                }
                if (velocity.Linear.y >=0)
                {
                    velocity.Linear.y = 0;
                }
            }).Run();
        }

        public void Hit(float3 force)
        {
            force.y = 0;
            Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation positon) =>
            {
                if (ball.id == 0)
                {
                    velocity.Linear += force;
                }
            }).Run();
        }

        private void HandleBreakShot()
        {
            if (isBreakShot)
            {
                float3 velocityCue = float3.zero;
                float3 positionCue = float3.zero;
                Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation positon) =>
                {
                    if (ball.id == 0)
                    {
                        velocityCue = velocity.Linear;
                        positionCue = positon.Value;
                    }
                }).WithoutBurst().Run();
                float3 positionHit;
                bool isHit;
                int count = 0;
                Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation positon) =>
                {
                    positionsBall[count] = positon.Value;
                    velocitysBall[count++] = velocity.Linear;
                }).WithoutBurst().Run();
                for (int i = 0; i < count; i++)
                {
                    int j = 0;
                    if (StaticFuntion.GetPowSizeVector2(velocitysBall[i]) > 0.00001f)
                    {
                        Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation positon) =>
                        {
                            if (i != j)
                            {
                                if (StaticFuntion.IsCollisionBall(positionsBall[i], positon.Value))
                                {
                                    StaticFuntion.VelocityAfterCollisionBall(positionsBall[i], positon.Value, velocitysBall[i], velocity.Linear, out isHit, out positionHit, ref velocitysBall[i], ref velocitysBall[j]);
                                    velocity.Linear.y = 0;
                                    if (isHit)
                                    {
                                        isBreakShot = false;
                                    }
                                }
                            }
                            j++;
                        }).WithoutBurst().Run();
                    }
                }
                count = 0;
                Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation positon) =>
                {
                    velocity.Linear = velocitysBall[count++];
                }).WithoutBurst().Run();
            }
        }

        private void HandleCheckTrack()
        {
            isBallStillRollingOnPack = false;
            Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation position) =>
            {
                if (position.Value.y < -3 && position.Value.y > -5)
                {
                    position.Value = positionTrack;
                    trackCountBall[ball.id] = trackCount++;
                    isBallStillRollingOnPack = true;
                }
                else if (position.Value.y < -5)
                {
                    float posZ = MIN_POSITION_TRACK_X + 0.325f * trackCountBall[ball.id];
                    if (position.Value.z > posZ + 0.05f)
                    {
                        isBallStillRollingOnPack = true;
                    }
                    if (position.Value.z <= posZ)
                    {
                        position.Value.z = posZ - 0.01f;

                        velocity.Linear.z = 0f;
                        velocity.Angular = float3.zero;
                    }
                    else
                    {
                        velocity.Linear.z = -1.5f;
                    }
                    velocity.Linear.x = 1.5f;
                }
                else
                if (position.Value.y < -1)
                {
                    isBallStillRollingOnPack = true;
                }
            }).WithoutBurst().Run();
        }

        private void CheckFirstHit()
        {
            if (firstHit)
            {
                float3 positionCueHit = float3.zero;
                float3 positionCue = float3.zero;
                float3 velocityCue = float3.zero;
                float3 posTargetBall = float3.zero;
                Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation position) =>
                {
                    if(ball.id == 0)
                    {
                        if (IsInPocket(position.Value, out float3 positionPocket))
                        {
                            if (position.Value.y > 2f)
                            {
                                position.Value.y = 0;
                            }
                        }
                        else
                        if (position.Value.y < 2f && position.Value.y > -0.1f)
                        {
                            position.Value.y = 3f;
                        }
                    }
                    else
                    if (ball.id == serialHitBall)
                    {
                        posTargetBall = position.Value;
                    }
                }).WithoutBurst().Run();
                Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation positon) =>
                {
                    if (ball.id == 0)
                    {
                        StaticFuntion.GetHitPositionBallToBall(positon.Value, posTargetBall, velocity.Linear, out bool isHit, out positionCueHit);
                        positionCue = positon.Value;
                        velocityCue = velocity.Linear;
                    }
                }).WithoutBurst().Run();
                if (serialHitBall > 0)
                {
                    float3 velocityRootOut = float3.zero;
                    Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation position) =>
                    {
                        if (ball.id == serialHitBall)
                        {
                            if (StaticFuntion.IsCollisionBall(positionCue, position.Value, StaticFuntion.GetSizeVector(velocityCue) * 0.01f))
                            {
                                StaticFuntion.VelocityAfterCollisionBall(positionCueHit, position.Value, velocityCue, float3.zero, out bool isHit, out float3 positonHit, ref velocityRootOut, ref velocity.Linear);
                                if (isHit)
                                {
                                    firstHit = false;
                                }
                            }
                        }
                    }).WithoutBurst().Run();

                    if (!firstHit)
                    {
                        Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation positon) =>
                        {
                            if (ball.id == 0)
                            {
                                velocity.Linear = velocityRootOut;
                                velocity.Angular = float3.zero;
                                positon.Value = positionCueHit + velocityRootOut * 0.0001f;
                            }
                        }).WithoutBurst().Run();
                    }
                }
                else
                {
                    if(StaticFuntion.GetPowSizeVector2(positionFirstHit - positionCue) <= StaticFuntion.GetPowSizeVector2(velocityCue * 0.02f))
                    {
                        Entities.ForEach((ref Ball ball, ref PhysicsVelocity velocity, ref Translation positon) =>
                        {
                            if (ball.id == 0)
                            {
                                positon.Value = positionFirstHit;
                                positon.Value.y = 0;
                            }
                        }).WithoutBurst().Run();
                        firstHit = false;
                    }
                }
            }
        }

        public void GetBallOutOfTrack(int ballId)
        {
            trackCount--;
            for (int i = 0; i < trackCountBall.Length; i++)
            {
                if (trackCountBall[i] > trackCountBall[ballId])
                {
                    trackCountBall[i]--;
                }
            }
        }

        public bool IsCollisionAnyBall(Ball ball,float3 position)
        {
            bool isTrue = false;
            Entities.ForEach((ref Ball ball1, ref Translation position1) =>
            {
                if (!isTrue && ball.id != ball1.id)
                {
                    isTrue = StaticFuntion.IsCollisionBall(position, position1.Value);
                }
            }).WithoutBurst().Run();
            return isTrue;
        }

        public bool IsAnyBallMove()
        {
            bool isAnyBallMove = false;
            Entities.ForEach((ref Ball ball, ref Translation position, ref PhysicsVelocity velocity) =>
            {
                if (position.Value.y > -5f)
                {
                    float powPower = StaticFuntion.GetPowSizeVector2(velocity.Linear);
                    float friction;
                    if (IsInPocket(position.Value, out float3 positionPocket))
                    {
                        positionPocket -= position.Value;
                        positionPocket.y = 0;
                        velocity.Linear += positionPocket*0.1f;
                        isAnyBallMove = true;
                    }
                    else
                    if (powPower < 0.00001f)
                    {
                        velocity.Linear.x = 0;
                        velocity.Linear.y = 0;
                        velocity.Angular = float3.zero;
                    }
                    else
                    {
                        friction = math.clamp(1f - 0.001f / powPower, 0f, 1f) * 0.997f;
                        velocity.Linear.x *= friction;
                        velocity.Linear.z *= friction;
                        isAnyBallMove = true;
                    }
                }
            }).WithoutBurst().Run();
            return isAnyBallMove;
        }

        private bool IsInPocket(float3 position, out float3 positionPocket)
        {
            bool isInPocket = false;
            float3 pos = float3.zero;
            Entities.ForEach((ref Pocket pocket, ref Translation currentPositionPocket, ref NonUniformScale scale) =>
            {
                if (!isInPocket)
                {
                    if (StaticFuntion.IsInPocket(position, currentPositionPocket.Value, scale.Value.x / 2f))
                    {
                        isInPocket = true;
                        pos = currentPositionPocket.Value;
                    }
                }
            }).WithoutBurst().Run();
            positionPocket = pos;
            return isInPocket;
        }

        private void UpdateAttach()
        {
            int i = 0;
            Entities.ForEach((ref Ball ball, ref Translation pos) =>
            {
                positionsBall[i++] = pos.Value;
            }).WithoutBurst().Run();
            i = 0;
            Entities.ForEach((ref Attach attach, ref Translation pos) =>
            {
                pos.Value = positionsBall[i++];
            }).WithoutBurst().Run();
        }
    }

}
