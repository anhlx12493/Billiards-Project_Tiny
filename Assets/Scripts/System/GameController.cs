using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Tiny.Input;
using Unity.Tiny.UI;
using Unity.Tiny;
using Unity.Physics;
using Unity.Tiny.Rendering;

namespace Billiards
{
    [UpdateAfter(typeof(UIController))]
    public class GameController : SystemBase
    {
        public const float BOARD_BORDER_TOP = 3.18f;
        public const float BOARD_BORDER_LEFT = -5f;
        public const float BOARD_BORDER_RIGHT = 5f;
        public const float BOARD_BORDER_BOTTOM = -2.87f;

        public const float BORDER_HAND_MOVE_TOP = 2.3f;
        public const float BORDER_HAND_MOVE_LEFT = -4.6f;
        public const float BORDER_HAND_MOVE_RIGHT = 4.6f;
        public const float BORDER_BREAK_SHOT_HAND_MOVE_RIGHT = -2.4f;
        public const float BORDER_HAND_MOVE_BOTTOM = -2.0f;

        private const float MAX_POWER = 22;

        public enum BallInHand {none, BreakShot, Free };

        private InputSystem Input;
        private bool isPowerUp;
        private bool isAdjustBar;
        private bool isAdjustBoard;
        private bool isShowGuide = true;
        private bool isDrag;
        private bool isBallInHandMoving;
        private bool isShoot;
        private bool isLastShoot;
        private bool isAnyBallMoving;
        private bool isSmoothRotating;
        private bool isCueBallInPocket;
        private bool isGlowOpacityUp;
        private bool isUpdateTarget = true;
        private bool isAbleShowGlow = true;
        private bool[] isTargetBalls = new bool[16];
        private float3 firstWorldPositionPower;
        private float3 lastWorldPositionAdjust;
        private float3 direction = new float3(1, 0, 0);
        private float3 targetDirection;
        private float3 mousePosition;
        private float3 positionCueBall;
        private float3 lastMousePosition;
        private float power;
        private float timeMouseUpCheckDrag;
        private float speedSmoothRotate;
        private float numberShowGlow = 19;
        private BallInHand ballInHand = BallInHand.BreakShot;

        protected override void OnStartRunning()
        {
            Input = World.GetExistingSystem<InputSystem>();
        }

        protected override void OnUpdate()
        {
            HandleInput();
            HandleRule();
            if (!isCueBallInPocket)
            {
                HandleMoveBallInHand();
                HandlePower();
                HandleAdjust();
                HandleGuide();
                HandleShoot();
                if (!isAnyBallMoving)
                {
                    HandleShowGlow();
                }
            }
        }

        private void HandleRule()
        {
            if (positionCueBall.y < -5f)
            {
                isCueBallInPocket = true;
                if (!isAnyBallMoving && !BallController.Instance.isBallStillRollingOnPack)
                {
                    isCueBallInPocket = false;
                    ballInHand = BallInHand.Free;
                    SetCueBall(new float3(0, 3, 0), float3.zero, float3.zero);
                    BallController.Instance.GetBallOutOfTrack(0);
                }
            }
            if (isUpdateTarget)
            {
                isUpdateTarget = false;
                int i = 0;
                Entities.ForEach((ref Ball ball, ref Translation position) =>
                {
                    if (position.Value.y > -1)
                    {
                        isTargetBalls[i++] = true;
                    }
                    else
                    {
                        isTargetBalls[i++] = false;
                    }
                }).WithoutBurst().Run();
                isTargetBalls[0] = false;
            }
            else
                if (!isAnyBallMoving && !BallController.Instance.isBallStillRollingOnPack)
            {
                isUpdateTarget = true;
                isAbleShowGlow = true;
            }
        }

        private void HandleInput()
        {
            mousePosition = UIController.WorldMousePosition;
            positionCueBall = GetPositionCueBall();
            if (Input.GetMouseButtonDown(0))
            {
                isDrag = false;
                timeMouseUpCheckDrag = 0;
            }
            else if (Input.GetMouseButton(0))
            {
                if (timeMouseUpCheckDrag > 0.2f || StaticFuntion.GetPowSizeVector2(lastMousePosition - mousePosition) > 0.03f)
                {
                    isDrag = true;
                }
                timeMouseUpCheckDrag += Time.DeltaTime;
            }
            isAnyBallMoving = BallController.Instance.IsAnyBallMove();
            lastMousePosition = mousePosition;
        }

        private void HandleGuide()
        {
            if (isShowGuide && !isBallInHandMoving && !isCueBallInPocket)
            {
                if (Input.GetMouseButtonUp(0) && !isDrag && !isPowerUp && !isAdjustBar && !isPowerUp)
                {
                    float3 closestPostion = new float3(1000, 0, 1000);
                    bool isHaveBall = false;
                    Entities.ForEach((ref Ball ball, ref Translation position) =>
                    {
                        if (ball.id != 0)
                        {
                            if (StaticFuntion.IsCollisionBall(position.Value, mousePosition))
                            {
                                isHaveBall = true;
                                if (StaticFuntion.GetPowSizeVector2(closestPostion - mousePosition) > StaticFuntion.GetPowSizeVector2(position.Value - mousePosition))
                                {
                                    closestPostion = position.Value;
                                }
                            }
                        }
                    }).WithoutBurst().Run();
                    if (isHaveBall)
                    {
                        targetDirection = closestPostion - positionCueBall;
                        isSmoothRotating = true;
                        speedSmoothRotate = StaticFuntion.GetAngle(direction, targetDirection) / 10f;
                    }
                    else
                    {
                        var posCueBall = positionCueBall;
                        targetDirection = mousePosition - posCueBall;
                        isSmoothRotating = true;
                        speedSmoothRotate = StaticFuntion.GetAngle(direction, targetDirection) / 10f;
                    }
                }
                ShowGuide(direction);
                if (isSmoothRotating)
                {
                    float currentAngle = StaticFuntion.GetAngle(direction, targetDirection);
                    if (math.abs(currentAngle) > math.abs(speedSmoothRotate))
                    {
                        StaticFuntion.RotateVectorWithoutSize2(ref direction, speedSmoothRotate);
                    }
                    else
                    {
                        direction = targetDirection;
                        isSmoothRotating = false;
                    }
                }
            }
            else
            {
                HideGuide();
            }
        }

        private void HandlePower()
        {
            float top;
            float left;
            float right;
            float bottom;
            float sizeZ = 4f;
            top = left = right = bottom = 0;
            bool isFirst = true;
            Entities.ForEach((ref UIObject uiObject, ref Translation position) =>
            {
                if (isFirst)
                {
                    isFirst = false;
                    top = uiObject.size.z / 2f + position.Value.z;
                    left = -uiObject.size.x / 2f + position.Value.x;
                    right = uiObject.size.x / 2f + position.Value.x;
                    bottom = -uiObject.size.z / 2f + position.Value.z;
                }
            }).WithoutBurst().Run();
            if (Input.GetMouseButtonDown(0))
            {
                if (mousePosition.x > left && mousePosition.x < right && mousePosition.z > bottom && mousePosition.z < top)
                {
                    isPowerUp = true;
                    firstWorldPositionPower = mousePosition;
                }
            }
            else if (Input.GetMouseButton(0))
            {
                if (isPowerUp)
                {
                    isFirst = true;
                    Entities.ForEach((ref Slider slider, ref NonUniformScale scale) =>
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            scale.Value.z = math.clamp((mousePosition.z - firstWorldPositionPower.z) / sizeZ + 1f, 0f, 1f);
                            power = (1 - scale.Value.z) * MAX_POWER;
                        }
                    }).WithoutBurst().Run();
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (isPowerUp)
                {
                    isPowerUp = false;
                    isShowGuide = false;
                    if (power > 0)
                    {
                        isShoot = true;
                        isLastShoot = true;
                        ballInHand = BallInHand.none;
                    }
                    isFirst = true;
                    Entities.ForEach((ref Slider slider, ref NonUniformScale scale) =>
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            scale.Value.z = 0.999f;
                        }
                    }).WithoutBurst().Run();
                }
            }
        }

        private void HandleAdjust()
        {
            if (isBallInHandMoving)
            {
                return;
            }
            float top;
            float left;
            float right;
            float bottom;
            float sizeZ;
            sizeZ = top = left = right = bottom = 0;
            int serial = 0;
            float adjustUp;
            Entities.ForEach((ref UIObject uiObject, ref Translation position) =>
            {
                if (serial++ == 1)
                {
                    top = uiObject.size.z / 2f + position.Value.z;
                    left = -uiObject.size.x / 2f + position.Value.x;
                    right = uiObject.size.x / 2f + position.Value.x;
                    bottom = -uiObject.size.z / 2f + position.Value.z;
                    sizeZ = uiObject.size.z;
                }
            }).WithoutBurst().Run();
            if (Input.GetMouseButtonDown(0))
            {
                if (mousePosition.x > left && mousePosition.x < right && mousePosition.z > bottom && mousePosition.z < top)
                {
                    isAdjustBar = true;
                    lastWorldPositionAdjust = mousePosition;
                }
                else if (!isAdjustBar && !isPowerUp)
                {
                    isAdjustBoard = true;
                    lastWorldPositionAdjust = mousePosition;
                }
            }
            else if (Input.GetMouseButton(0))
            {
                if (isAdjustBar)
                {
                    serial = 0;
                    adjustUp = mousePosition.z - lastWorldPositionAdjust.z;
                    StaticFuntion.RotateVectorWithoutSize2(ref direction, adjustUp * 0.1f);
                    lastWorldPositionAdjust = mousePosition;
                }
                else if (isAdjustBoard)
                {
                    adjustUp = StaticFuntion.GetAngle(lastWorldPositionAdjust - positionCueBall, mousePosition - positionCueBall);
                    StaticFuntion.RotateVectorWithoutSize2(ref direction, adjustUp * 0.1f);
                    lastWorldPositionAdjust = mousePosition;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isAdjustBoard = isAdjustBar = false;
            }
        }

        private void HandleMoveBallInHand()
        {
            switch (ballInHand)
            {
                case BallInHand.BreakShot:
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (StaticFuntion.IsCollisionBall(mousePosition, positionCueBall, 0.2f))
                        {
                            isBallInHandMoving = true;
                            ShowHandMoveFeild();
                        }
                        HideHandHold();
                    }
                    else if (Input.GetMouseButton(0))
                    {
                        float3 targetPosition = mousePosition;
                        if (isBallInHandMoving && isDrag)
                        {
                            if (targetPosition.x < BORDER_HAND_MOVE_LEFT)
                            {
                                targetPosition.x = BORDER_HAND_MOVE_LEFT;
                            }
                            if (targetPosition.x > BORDER_BREAK_SHOT_HAND_MOVE_RIGHT)
                            {
                                targetPosition.x = BORDER_BREAK_SHOT_HAND_MOVE_RIGHT;
                            }
                            if (targetPosition.z < BORDER_HAND_MOVE_BOTTOM)
                            {
                                targetPosition.z = BORDER_HAND_MOVE_BOTTOM;
                            }
                            if (targetPosition.z > BORDER_HAND_MOVE_TOP)
                            {
                                targetPosition.z = BORDER_HAND_MOVE_TOP;
                            }
                            SetPositionCueBall(targetPosition);
                            positionCueBall = targetPosition;
                            ShowHandMove();
                        }
                    }
                    else if (Input.GetMouseButtonUp(0))
                    {
                        isBallInHandMoving = false;
                        HideHandMoveFeild();
                    }
                    else
                    {
                        ShowHandHold();
                        HideHandMove();
                    }
                    break;
                case BallInHand.Free:
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (StaticFuntion.IsCollisionBall(mousePosition, positionCueBall, 0.2f))
                        {
                            isBallInHandMoving = true;
                            ShowHandMoveFeild();
                        }
                        HideHandHold();
                    }
                    else if (Input.GetMouseButton(0))
                    {
                        float3 targetPosition = mousePosition;
                        if (isBallInHandMoving && isDrag)
                        {
                            if (targetPosition.x < BORDER_HAND_MOVE_LEFT)
                            {
                                targetPosition.x = BORDER_HAND_MOVE_LEFT;
                            }
                            if (targetPosition.x > BORDER_HAND_MOVE_RIGHT)
                            {
                                targetPosition.x = BORDER_HAND_MOVE_RIGHT;
                            }
                            if (targetPosition.z < BORDER_HAND_MOVE_BOTTOM)
                            {
                                targetPosition.z = BORDER_HAND_MOVE_BOTTOM;
                            }
                            if (targetPosition.z > BORDER_HAND_MOVE_TOP)
                            {
                                targetPosition.z = BORDER_HAND_MOVE_TOP;
                            }
                            bool isHitAnyBall = false;
                            Entities.ForEach((ref Ball ball, ref Translation position) =>
                            {
                                if (ball.id != 0)
                                {
                                    if (!isHitAnyBall && StaticFuntion.IsCollisionBall(targetPosition, position.Value))
                                    {
                                        isHitAnyBall = true;
                                    }
                                }
                            }).WithoutBurst().Run();
                            if (!isHitAnyBall)
                            {
                                SetPositionCueBall(targetPosition);
                                positionCueBall = targetPosition;
                            }
                            ShowHandMove();
                        }
                    }
                    else if (Input.GetMouseButtonUp(0))
                    {
                        isBallInHandMoving = false;
                        HideHandMoveFeild();
                    }
                    else
                    {
                        ShowHandHold();
                        HideHandMove();
                    }
                    break;
            }
        }

        private void HandleShoot()
        {
            if (isAnyBallMoving)
            {
                isShoot = false;
            }
            else
            {
                if (isShoot)
                {
                    if (!HittingCue())
                    {
                        isShoot = false;
                        HideCue();
                        BallController.Instance.Hit(StaticFuntion.GetResizeVector2(direction, power));
                        power = 0;
                        HideAllGlows();
                    }
                }
                else
                {
                    if (isBallInHandMoving)
                    {
                        HideCue();
                    }
                    else
                    {
                        ShowCue();
                    }
                    if (!isShowGuide)
                    {
                        if (isLastShoot)
                        {
                            isLastShoot = false;
                            direction = GetPositionClosestTargetBall() - positionCueBall;
                        }
                        isSmoothRotating = false;
                        isShowGuide = true;
                        BallController.Instance.firstHit = true;
                        ShowCue();
                        HideCue();
                        ShowGuide(direction);
                        HideGuide();
                    }
                }
            }
        }


        private void HandleShowGlow()
        {
            if (isAbleShowGlow)
            {
                Entity mat = Entity.Null;
                if (isGlowOpacityUp)
                {
                    if (numberShowGlow > 0)
                    {
                        numberShowGlow -= 0.5f;
                    }
                    else
                    {
                        isGlowOpacityUp = false;
                    }
                }
                else
                {
                    if (numberShowGlow < 19)
                    {
                        numberShowGlow += 0.5f;
                    }
                    else
                    {
                        isGlowOpacityUp = true;
                    }
                }
                int i = 0;
                Entities.ForEach((ref GlowShare glow, ref MeshRenderer meshRenderer) =>
                {
                    if (i++ == (int)numberShowGlow)
                    {
                        mat = meshRenderer.material;
                    }

                }).WithoutBurst().Run();
                i = 0;
                Entities.ForEach((ref Glow glow, ref MeshRenderer meshRenderer, ref Translation position) =>
                {
                    if (isTargetBalls[i++])
                    {
                        meshRenderer.material = mat;
                        position.Value.y = 0;
                    }
                    else
                    {
                        position.Value.y = 1000;
                    }
                }).WithoutBurst().Run();
            }
        }

        private void HideAllGlows()
        {
            isAbleShowGlow = false;
            numberShowGlow = 19;
            isGlowOpacityUp = true;
            Entities.ForEach((ref Glow glow, ref Translation position) =>
            {
                position.Value.y = 1000;
            }).WithoutBurst().Run();
        }

        private void ShowHandMoveFeild()
        {
            switch (ballInHand)
            {
                case BallInHand.BreakShot:
                    Entities.ForEach((ref HandMoveFeild moveFeild, ref Translation position, ref NonUniformScale scale) =>
                    {
                        position.Value.x = (BORDER_BREAK_SHOT_HAND_MOVE_RIGHT + BORDER_HAND_MOVE_LEFT) / 2f;
                        position.Value.z = (BORDER_HAND_MOVE_TOP + BORDER_HAND_MOVE_BOTTOM) / 2f;
                        scale.Value.x = (BORDER_BREAK_SHOT_HAND_MOVE_RIGHT - BORDER_HAND_MOVE_LEFT) * 0.1f;
                        scale.Value.z = (BORDER_HAND_MOVE_TOP - BORDER_HAND_MOVE_BOTTOM) * 0.1f;
                    }).WithoutBurst().Run();
                    break;
                case BallInHand.Free:
                    Entities.ForEach((ref HandMoveFeild moveFeild, ref Translation position, ref NonUniformScale scale) =>
                    {
                        position.Value.x = (BORDER_HAND_MOVE_RIGHT + BORDER_HAND_MOVE_LEFT) / 2f;
                        position.Value.z = (BORDER_HAND_MOVE_TOP + BORDER_HAND_MOVE_BOTTOM) / 2f;
                        scale.Value.x = (BORDER_HAND_MOVE_RIGHT - BORDER_HAND_MOVE_LEFT) * 0.1f;
                        scale.Value.z = (BORDER_HAND_MOVE_TOP - BORDER_HAND_MOVE_BOTTOM) * 0.1f;
                    }).WithoutBurst().Run();
                    break;
            }
        }

        private void HideHandMoveFeild()
        {
            switch (ballInHand)
            {
                case BallInHand.none:
                    break;
                default:
                    Entities.ForEach((ref HandMoveFeild moveFeild, ref Translation position) =>
                    {
                        position.Value.x = 1000;
                    }).WithoutBurst().Run();
                    break;
            }
        }

        private void ShowHandHold()
        {
            switch (ballInHand)
            {
                case BallInHand.none:
                    break;
                default:
                    Entities.ForEach((ref Hand hand, ref Translation position) =>
                    {
                        position.Value = positionCueBall;
                    }).WithoutBurst().Run();
                    break;
            }
        }

        private void HideHandHold()
        {
            switch (ballInHand)
            {
                case BallInHand.none:
                    break;
                default:
                    Entities.ForEach((ref Hand hand, ref Translation position) =>
                    {
                        position.Value.x = 1000;
                    }).WithoutBurst().Run();
                    break;
            }
        }

        private void ShowHandMove()
        {
            switch (ballInHand)
            {
                case BallInHand.none:
                    break;
                default:
                    Entities.ForEach((ref HandMove hand, ref Translation position) =>
                    {
                        position.Value = positionCueBall;
                        position.Value.y = 5;
                    }).WithoutBurst().Run();
                    break;
            }
        }

        private void HideHandMove()
        {
            switch (ballInHand)
            {
                case BallInHand.none:
                    break;
                default:
                    Entities.ForEach((ref HandMove hand, ref Translation position) =>
                    {
                        position.Value.x = 1000;
                    }).WithoutBurst().Run();
                    break;
            }
        }



        private float3 GetPositionCueBall()
        {
            float3 result = float3.zero;
            Entities.ForEach((ref Ball ball, ref Translation position) =>
            {
                if (ball.id == 0)
                {
                    result = position.Value;
                }
            }).Run();
            return result; 
        }

        private void SetPositionCueBall(float3 position)
        {
            Entities.ForEach((ref Ball ball, ref Translation currentPosition) =>
            {
                if (ball.id == 0)
                {
                    currentPosition.Value = position;
                }
            }).Run();
        }

        private void SetCueBall(float3 position,float3 linearVelocity,float3 angularVelocity)
        {
            Entities.ForEach((ref Ball ball, ref Translation currentPosition, ref PhysicsVelocity velocity) =>
            {
                if (ball.id == 0)
                {
                    currentPosition.Value = position;
                    velocity.Linear = linearVelocity;
                    velocity.Angular = angularVelocity;
                }
            }).Run();
        }

        private void ShowCue()
        {
            Entities.ForEach((ref Cue cue, ref Translation position, ref Rotation rotation) =>
            {
                if (cue.isSlide)
                {
                    position.Value.x = -(power / MAX_POWER) * 2f;
                }
                else
                {
                    position.Value = positionCueBall;
                    rotation.Value = quaternion.LookRotation(new float3(-direction.z, 0, direction.x), new float3(0, 1, 0));
                }
            }).WithoutBurst().Run();
        }

        private void HideCue()
        {
            Entities.ForEach((ref Cue cue, ref Translation position) =>
            {
                if (!cue.isSlide)
                {
                    position.Value.x = 1000;
                }
            }).WithoutBurst().Run();
        }

        private bool HittingCue()
        {
            bool isHitting = true;
            Entities.ForEach((ref Cue cue, ref Translation position) =>
            {
                if (cue.isSlide)
                {
                    position.Value.x += (power / MAX_POWER) * 0.25f;
                    if (position.Value.x >= 0)
                    {
                        position.Value.x = 0;
                        isHitting = false;
                    }
                }
            }).WithoutBurst().Run();
            return isHitting;
        }

        private void ShowGuide(float3 force)
        {
            force.y = 0;
            float3 posTargetBall = float3.zero;
            float3 hitPosition = float3.zero;
            bool isHit = false;

            float nearestDistance = float.MaxValue;
            float currentDistance;
            bool isCurrentHit;
            float3 currentHitPosition;
            BallController.Instance.serialHitBall = -1;
            float3 positionCheck;
            Entities.ForEach((ref Ball ball, ref Translation position) =>
            {
                if (ball.id != 0)
                {
                    if (StaticFuntion.IsCollisionBall(positionCueBall, position.Value, 0.0001f))
                    {
                        positionCheck = StaticFuntion.GetResizeVector2(positionCueBall - position.Value, StaticFuntion.diameterBall + 0.0001f) + position.Value;
                    }
                    else
                    {
                        positionCheck = positionCueBall;
                    }
                    StaticFuntion.GetHitPositionBallToBall(positionCheck, position.Value, force, out isCurrentHit, out currentHitPosition);
                    if (isCurrentHit)
                    {
                        isHit = true;
                        currentDistance = StaticFuntion.GetPowSizeVector2(currentHitPosition - positionCheck);
                        if (position.Value.y > -0.32f && nearestDistance > currentDistance)
                        {
                            nearestDistance = currentDistance;
                            hitPosition = currentHitPosition;
                            posTargetBall = position.Value;
                            BallController.Instance.serialHitBall = ball.id;
                        }
                    }
                }
            }).WithoutBurst().Run();
            float3 start = positionCueBall;
            float3 end = positionCueBall + StaticFuntion.GetResizeVector2(force, 100f);
            start.y = end.y = 20;
            SphereCast(start, end, 0.159f, out bool isHaveHit, out ColliderCastHit hit);
            if (isHit)
            {
                if (isHaveHit)
                {
                    float3 hitBoardPosition = hit.Position + StaticFuntion.GetResizeVector2(hit.SurfaceNormal, 0.16f);
                    currentDistance = StaticFuntion.GetPowSizeVector2(hitBoardPosition - positionCueBall);
                    if (nearestDistance > currentDistance)
                    {
                        nearestDistance = currentDistance;
                        hitPosition = hitBoardPosition;
                        BallController.Instance.serialHitBall = -1;
                        isHit = false;
                    }
                }
            }
            else
            {
                if (isHaveHit)
                {
                    hitPosition = hit.Position + StaticFuntion.GetResizeVector2(hit.SurfaceNormal, 0.16f);
                }
                else
                {
                    hitPosition = positionCueBall;
                }
            }
            BallController.Instance.positionFirstHit = hitPosition;
            if (isHit)
            {
                float3 velocityRootOut = float3.zero;
                float3 velocityTargetOut = float3.zero;
                currentHitPosition = float3.zero;
                Entities.ForEach((ref Ball ball, ref Translation position) =>
                {
                    if (ball.id == BallController.Instance.serialHitBall)
                    {
                        StaticFuntion.VelocityAfterCollisionBall(positionCueBall, position.Value, force, float3.zero, out isCurrentHit, out currentHitPosition, ref velocityRootOut, ref velocityTargetOut);
                    }
                }).WithoutBurst().Run();
                Entities.ForEach((ref Guide guide, ref Translation position, ref Rotation rotation, ref NonUniformScale scale) =>
                {
                    switch (guide.id)
                    {
                        case Guide.ID.hitPoint:
                            position.Value = hitPosition;
                            break;
                        case Guide.ID.LineCueIncidence:
                            DrawLine(ref position, ref rotation, ref scale, positionCueBall, hitPosition);
                            break;
                        case Guide.ID.LineCueReflection:
                            float3 cueReflectPosition = hitPosition + velocityRootOut * (1f / StaticFuntion.GetSizeVector(force));

                            DrawLine(ref position, ref rotation, ref scale, hitPosition, cueReflectPosition);
                            break;
                        case Guide.ID.LineBeHit:
                            float3 reflectPosition = posTargetBall + velocityTargetOut * (1f / StaticFuntion.GetSizeVector(force));

                            DrawLine(ref position, ref rotation, ref scale, posTargetBall, reflectPosition);
                            break;
                    }
                    position.Value.y = 1;
                }).WithoutBurst().Run();
            }
            else
            {
                if (isHaveHit)
                {
                    Entities.ForEach((ref Guide guide, ref Translation position, ref Rotation rotation, ref NonUniformScale scale) =>
                    {
                        switch (guide.id)
                        {
                            case Guide.ID.hitPoint:
                                position.Value = hitPosition;
                                break;
                            case Guide.ID.LineCueIncidence:
                                DrawLine(ref position, ref rotation, ref scale, positionCueBall, hitPosition);
                                break;
                            case Guide.ID.LineCueReflection:
                                position.Value = new float3(1000, position.Value.y, 1000);
                                break;
                            case Guide.ID.LineBeHit:
                                position.Value = new float3(1000, position.Value.y, 1000);
                                break;
                        }
                        position.Value.y = 1;
                    }).WithoutBurst().Run();
                }
                else
                {
                    Entities.ForEach((ref Guide guide, ref Translation position, ref Rotation rotation, ref NonUniformScale scale) =>
                    {
                        position.Value = new float3(1000, position.Value.y, 1000);
                    }).WithoutBurst().Run();
                }
            }
        }

        private void HideGuide()
        {
            Entities.ForEach((ref Guide guide, ref Translation position) =>
            {
                position.Value = new float3(1000, position.Value.y, 1000);
            }).WithoutBurst().Run();
        }

        public float3 GetPositionClosestTargetBall()
        {
            float3 positionClosest = new float3(1000, 0, 1000);
            Entities.ForEach((ref Ball ball, ref Translation position) =>
            {
                if (ball.id != 0 && position.Value.y > -1f)
                {
                    if (StaticFuntion.GetPowSizeVector2(positionClosest - positionCueBall) > StaticFuntion.GetPowSizeVector2(position.Value - positionCueBall))
                    {
                        positionClosest = position.Value;
                    }
                }
            }).WithoutBurst().Run();
            return positionClosest;
        }

        private void DrawLine(ref Translation position, ref Rotation rotation, ref NonUniformScale scale, float3 pos1, float3 pos2)
        {
            position.Value = pos1 + (pos2 - pos1) / 2;
            scale.Value.x = math.sqrt(math.pow(pos2.x - pos1.x, 2) + math.pow(pos2.z - pos1.z, 2)) / 10f;
            float3 force = pos2 - pos1;
            rotation.Value = quaternion.LookRotation(new float3(-force.z, 0, force.x), new float3(0, 1, 0));
        }

        public unsafe Entity SphereCast(float3 RayFrom, float3 RayTo, float radius, out bool isHaveHit, out ColliderCastHit hit)
        {
            var physicsWorldSystem = World.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
            var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

            var filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                GroupIndex = 0
            };

            SphereGeometry sphereGeometry = new SphereGeometry() { Center = float3.zero, Radius = radius };
            BlobAssetReference<Collider> sphereCollider = SphereCollider.Create(sphereGeometry, filter);

            ColliderCastInput input = new ColliderCastInput()
            {
                Collider = (Collider*)sphereCollider.GetUnsafePtr(),
                Orientation = quaternion.identity,
                Start = RayFrom,
                End = RayTo
            };

            isHaveHit = collisionWorld.CastCollider(input, out hit);
            if (isHaveHit)
            {
                // see hit.Position
                // see hit.SurfaceNormal
                Entity e = physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                return e;
            }

            sphereCollider.Dispose();

            return Entity.Null;
        }
    }
}
