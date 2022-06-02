using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Tiny.Input;
using Unity.Tiny.UI;
using Unity.Tiny;
using Unity.Physics;
using Unity.Tiny.Rendering;
using Unity.Tiny.Text;
using System.Collections.Generic;

namespace Billiards
{
    [UpdateAfter(typeof(UIController))]
    public class GameController : SystemBase, PhysicsEvent
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

        private const float MAX_POWER = 0.06f;
        private const float MAX_TIME = 30f;

        public enum BallInHand { none, BreakShot, Free };

        public static GameController Instance;


        public bool isPlayer = true;
        public bool IsInteractive { get; private set; }
        public bool IsHitWrongBall { get; private set; }
        public bool IsTiming { get; private set; }
        public bool IsTimeOut { get; private set; }

        private InputSystem Input;
        private bool isStart = true;
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
        private bool isAbleShowGlow = true;
        private bool isShowingMessage;
        private bool isMessageUp = true;
        private bool isStartTimeDown;
        private bool isAimWrongBall;
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
        private float numberShowGlow = 19;
        private float timeHangOnMessage;
        private float currentScaleTimeDown;
        private float maxScaleTimeDown;
        private float currentTimeTurnPlay;
        private int serialFirstHit;
        private BallInHand ballInHand = BallInHand.BreakShot;
        public BotInput botInput;

        protected override void OnStartRunning()
        {
            Instance = this;
            Input = World.GetExistingSystem<InputSystem>();
        }

        protected override void OnUpdate()
        {
            HandleInput();
            HandleRule();
            HandleMoveBallInHand();
            HandlePower();
            HandleAdjust();
            HandleGuide();
            HandleShoot();
            HandleShowGlow();
            HandleMessage();
            HandleTimeDown();

            HandleEndUpdate();
        }

        private void HandleRule()
        {
        }

        private void HandleInput()
        {
            if (isStart)
            {
                Physics.Instance.AddEventListener(this);
            }
            mousePosition = UIController.WorldMousePosition;
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
            positionCueBall = GetPositionCueBall();
            isAnyBallMoving = Physics.Instance.IsAnyBallMoving;
            lastMousePosition = mousePosition;
            isCueBallInPocket = Physics.Instance.IsBallInPocket(0);
        }

        private void HandleGuide()
        {
            int i;
            if (IsInteractive && isShowGuide && !isBallInHandMoving && !isCueBallInPocket)
            {
                if (isPlayer)
                {
                    if (Input.GetMouseButtonUp(0) && !isDrag && !isPowerUp && !isAdjustBar && !isPowerUp)
                    {
                        float3 closestPostion = new float3(1000, 0, 1000);
                        bool isHaveBall = false;
                        i = 0;
                        Entities.ForEach((ref Ball ball, ref Translation position) =>
                        {
                            if (i++ != 0)
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
                        }
                        else
                        {
                            targetDirection = mousePosition - positionCueBall;
                        }
                        isSmoothRotating = true;
                    }
                    ShowGuide(direction);
                }
                else
                {
                    if (botInput.isClick)
                    {
                        targetDirection = botInput.position - positionCueBall;
                        isSmoothRotating = true;
                    }
                    HideGuide();
                }
                if (isSmoothRotating)
                {
                    float currentAngle = StaticFuntion.GetAngle(direction, targetDirection);
                    if (math.abs(currentAngle) > 0.001f)
                    {
                        StaticFuntion.RotateVectorWithoutSize2(ref direction, currentAngle * 0.3f);
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
            if (isPlayer)
            {
                if (!IsInteractive)
                    return;
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
                            if (IsTiming)
                            {
                                IsInteractive = false;
                                IsTiming = false;
                                isShoot = true;
                            }
                            isLastShoot = true;
                            ballInHand = BallInHand.none;
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
            }
            else
            {
                power = botInput.scalePower * MAX_POWER;
                if (botInput.isShot)
                {
                    if (power > 0)
                    {
                        if (IsTiming)
                        {
                            IsInteractive = false;
                            IsTiming = false;
                            isShoot = true;
                        }
                        isLastShoot = true;
                        ballInHand = BallInHand.none;
                    }
                }
            }
        }

        private void HandleAdjust()
        {
            if (isBallInHandMoving && !isPlayer && !IsInteractive)
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
            if (isPlayer && Input.GetMouseButtonDown(0))
            {
                if (mousePosition.x > left && mousePosition.x < right && mousePosition.z > bottom && mousePosition.z < top)
                {
                    isAdjustBar = true;
                    lastWorldPositionAdjust = mousePosition;
                    targetDirection = direction;
                }
                else if (!isAdjustBar && !isPowerUp)
                {
                    isAdjustBoard = true;
                    lastWorldPositionAdjust = mousePosition;
                    targetDirection = direction;
                }
            }
            else if (isPlayer && Input.GetMouseButton(0))
            {
                if (isAdjustBar)
                {
                    serial = 0;
                    adjustUp = mousePosition.z - lastWorldPositionAdjust.z;
                    StaticFuntion.RotateVectorWithoutSize2(ref targetDirection, adjustUp * 0.45f);
                    lastWorldPositionAdjust = mousePosition;
                    isSmoothRotating = true;
                }
                else if (isAdjustBoard)
                {
                    adjustUp = StaticFuntion.GetAngle(lastWorldPositionAdjust - positionCueBall, mousePosition - positionCueBall);
                    StaticFuntion.RotateVectorWithoutSize2(ref targetDirection, adjustUp * 0.45f);
                    lastWorldPositionAdjust = mousePosition;
                    isSmoothRotating = true;
                }
            }
            else if (isPlayer && Input.GetMouseButtonUp(0))
            {
                isAdjustBoard = isAdjustBar = false;
            }
        }

        private void HandleMoveBallInHand()
        {
            if (isPlayer && IsInteractive)
            {
                int i;
                switch (ballInHand)
                {
                    case BallInHand.none:
                        HideHandHold();
                        HideHandMoveFeild();
                        HideHandMove();
                        break;
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
                                i = 0;
                                Entities.ForEach((ref Ball ball, ref Translation position) =>
                                {
                                    if (i++ != 0)
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
            else
            {
                HideHandHold();
                HideHandMoveFeild();
                HideHandMove();
            }
        }

        private void HandleTimeDown()
        {
            if (isStart)
            {
                maxScaleTimeDown = 0;
                Entities.ForEach((ref TimeDown timeDown, ref NonUniformScale scale, ref SpriteRenderer spriteRenderer) =>
                {
                    if (timeDown.subject == TimeDown.Subject.you)
                    {
                        maxScaleTimeDown += scale.Value.x;
                    }
                    timeDown.size = scale.Value.x;
                    scale.Value.x = 0;
                }).WithoutBurst().Run();
            }
            else
            {
                if (isStartTimeDown)
                {
                    isStartTimeDown = false;
                    ActiveTimeDown();
                    currentTimeTurnPlay = 0;
                    currentScaleTimeDown = 0;
                    IsTiming = true;
                }
                if (IsTiming)
                {
                    currentTimeTurnPlay += Time.DeltaTime;
                    if (currentTimeTurnPlay >= MAX_TIME)
                    {
                        IsTiming = false;
                        IsTimeOut = true;
                        OffTimeDown();
                        return;
                    }
                    float subValue = (currentTimeTurnPlay / MAX_TIME) * maxScaleTimeDown - currentScaleTimeDown;
                    if (subValue < 0)
                        subValue = 0;
                    currentScaleTimeDown += subValue;
                    Entities.ForEach((ref TimeDown timeDown, ref NonUniformScale scale, ref SpriteRenderer spriteRenderer) =>
                    {
                        if (isPlayer && timeDown.subject == TimeDown.Subject.you || !isPlayer && timeDown.subject == TimeDown.Subject.bot)
                        {
                            float scaleColor = currentScaleTimeDown / maxScaleTimeDown;
                            if (scaleColor < 0.5f)
                            {
                                spriteRenderer.Color = math.lerp(Colors.Green.Value, Colors.Yellow.Value, scaleColor * 2);
                            }
                            else
                            {
                                spriteRenderer.Color = math.lerp(Colors.Yellow.Value, Colors.Red.Value, scaleColor * 2f - 1f);
                            }
                            if (subValue > 0f)
                            {
                                if (scale.Value.x >= subValue)
                                {
                                    scale.Value.x -= subValue;
                                    subValue = 0;
                                }
                                else
                                {
                                    subValue -= scale.Value.x;
                                    scale.Value.x = 0;
                                }
                            }
                        }
                    }).WithoutBurst().Run();
                }
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
                        HitCueBall(StaticFuntion.GetResizeVector2(direction, power));
                        power = 0;
                        HideAllGlows();
                    }
                }
                else
                {
                    if (IsInteractive)
                    {
                        ShowCue();
                    }
                    else
                    {
                        HideCue();
                    }
                    if (!isShowGuide)
                    {
                        if (isLastShoot)
                        {
                            isLastShoot = false;
                        }
                        isSmoothRotating = false;
                        isShowGuide = true;
                        ShowCue();
                        HideCue();
                        HideGuide();
                    }
                }
            }
        }


        private void HandleShowGlow()
        {
            if (isAnyBallMoving && !IsInteractive)
            {
                return;
            }
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
                    if (isPlayer && isTargetBalls[i++])
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

        private void HandleMessage()
        {
            if (isShowingMessage)
            {
                if (isMessageUp)
                {
                    Entities.ForEach((ref Bubble bubble, ref Translation position) =>
                    {
                        if (position.Value.z < 0.75f)
                        {
                            position.Value.z += 0.02f;
                        }
                        else
                        {
                            if (timeHangOnMessage > 0)
                            {
                                timeHangOnMessage -= Time.DeltaTime;
                            }
                            else
                            {
                                isMessageUp = false;
                            }
                        }
                    }).WithoutBurst().Run();
                }
                else
                {
                    Entities.ForEach((ref Bubble bubble, ref Translation position) =>
                    {
                        if (position.Value.z > 0f)
                        {
                            position.Value.z -= 0.02f;
                        }
                        else
                        {
                            isMessageUp = true;
                            isShowingMessage = false;
                            timeHangOnMessage = 1f;
                        }
                    }).WithoutBurst().Run();
                }
            }
        }

        private void HandleEndUpdate()
        {
            botInput.isClick = false;
            botInput.isShot = false;
            isStart = false;
        }

        private void HitCueBall(float3 force)
        {
            bool isCueBall = true;
            Entities.ForEach((ref Ball ball) =>
            {
                if (isCueBall)
                {
                    isCueBall = false;
                    ball.addForce = force;
                }
            }).WithBurst().Run();
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
            Entities.ForEach((ref HandMoveFeild moveFeild, ref Translation position) =>
            {
                position.Value.x = 1000;
            }).WithoutBurst().Run();
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
                    Entities.ForEach((ref Hand hand, ref Translation position) =>
                    {
                        position.Value.x = 1000;
                    }).WithoutBurst().Run();
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
            Entities.ForEach((ref HandMove hand, ref Translation position) =>
            {
                position.Value.x = 1000;
            }).WithoutBurst().Run();
        }



        private float3 GetPositionCueBall()
        {
            float3 result = float3.zero;
            bool isCueBall = true;
            Entities.ForEach((ref Ball ball, ref Translation position) =>
            {
                if (isCueBall)
                {
                    isCueBall = false;
                    result = position.Value;
                }
            }).Run();
            return result;
        }

        private void SetPositionCueBall(float3 position)
        {
            Physics.Instance.SetPositionCueBall(position);
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
            float3 positionCheck;
            int i = 0;
            int serialHitBall = -1;
            Entities.ForEach((ref Ball ball, ref Translation position) =>
            {
                if (i != 0)
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
                            serialHitBall = i;
                        }
                    }
                }
                i++;
            }).WithoutBurst().Run();
            float3 start = positionCueBall;
            float3 end = positionCueBall + StaticFuntion.GetResizeVector2(force, 100f);
            start.y = end.y = 20;
            Physics.Instance.GetRaycastHitPositionBallToBoardLine(positionCueBall, force, out bool isHaveHit, out float3 positionHit);
            if (isHit)
            {
                if (isHaveHit)
                {
                    currentDistance = StaticFuntion.GetPowSizeVector2(positionHit - positionCueBall);
                    if (nearestDistance > currentDistance)
                    {
                        nearestDistance = currentDistance;
                        hitPosition = positionHit;
                        isHit = false;
                    }
                }
            }
            else
            {
                if (isHaveHit)
                {
                    hitPosition = positionHit;
                }
                else
                {
                    hitPosition = positionCueBall;
                }
            }
            if (isHit)
            {
                float3 velocityRootOut = float3.zero;
                float3 velocityTargetOut = float3.zero;
                currentHitPosition = float3.zero;
                i = 0;
                Entities.ForEach((ref Ball ball, ref Translation position) =>
                {
                    if (i++ == serialHitBall)
                    {
                        StaticFuntion.VelocityAfterCollisionBall(positionCueBall, position.Value, force, out isCurrentHit, out currentHitPosition, ref velocityRootOut, ref velocityTargetOut);
                    }
                }).WithoutBurst().Run();
                if (serialHitBall > 0 && !isTargetBalls[serialHitBall])
                {
                    isAimWrongBall = true;
                }
                else
                {
                    isAimWrongBall = false;
                }
                Entities.ForEach((ref Guide guide, ref Translation position, ref Rotation rotation, ref NonUniformScale scale) =>
                {
                    switch (guide.id)
                    {
                        case Guide.ID.hitPoint:
                            if (isAimWrongBall)
                            {
                                position.Value.y = 1000;
                            }
                            else
                            {
                                position.Value = hitPosition;
                                position.Value.y = 1;
                            }
                            break;
                        case Guide.ID.hitWrongPoint:
                            if (isAimWrongBall)
                            {
                                position.Value = hitPosition;
                                position.Value.y = 1.1f;
                            }
                            else
                            {
                                position.Value.y = 1000;
                            }
                            break;
                        case Guide.ID.LineCueIncidence:
                            DrawLine(ref position, ref rotation, ref scale, positionCueBall, hitPosition);
                            position.Value.y = 1;
                            break;
                        case Guide.ID.LineCueReflection:
                            if (isAimWrongBall)
                            {
                                position.Value.y = 1000;
                            }
                            else
                            {
                                float3 cueReflectPosition = hitPosition + velocityRootOut * (1f / StaticFuntion.GetSizeVector(force));
                                DrawLine(ref position, ref rotation, ref scale, hitPosition, cueReflectPosition);
                                position.Value.y = 1;
                            }
                            break;
                        case Guide.ID.LineBeHit:
                            if (isAimWrongBall)
                            {
                                position.Value.y = 1000;
                            }
                            else
                            {
                                float3 reflectPosition = posTargetBall + velocityTargetOut * (1f / StaticFuntion.GetSizeVector(force));
                                DrawLine(ref position, ref rotation, ref scale, posTargetBall, reflectPosition);
                                position.Value.y = 1;
                            }
                            break;
                    }
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
                                position.Value.y = 1;
                                break;
                            case Guide.ID.hitWrongPoint:
                                position.Value.y = 1000;
                                break;
                            case Guide.ID.LineCueIncidence:
                                DrawLine(ref position, ref rotation, ref scale, positionCueBall, hitPosition);
                                position.Value.y = 1;
                                break;
                            case Guide.ID.LineCueReflection:
                                position.Value.y = 1000;
                                break;
                            case Guide.ID.LineBeHit:
                                position.Value.y = 1000;
                                break;
                        }
                    }).WithoutBurst().Run();
                }
                else
                {
                    Entities.ForEach((ref Guide guide, ref Translation position, ref Rotation rotation, ref NonUniformScale scale) =>
                    {
                        position.Value.y = 1000;
                    }).WithoutBurst().Run();
                }
            }
        }

        private void ActiveTimeDown()
        {
            Entities.ForEach((ref TimeDown timeDown, ref NonUniformScale scale, ref SpriteRenderer spriteRenderer) =>
            {
                if (isPlayer && timeDown.subject == TimeDown.Subject.you || !isPlayer && timeDown.subject == TimeDown.Subject.bot)
                {
                    scale.Value.x = timeDown.size;
                    spriteRenderer.Color = Colors.Green.Value;
                }
                else
                {
                    scale.Value.x = 0f;
                }
            }).WithoutBurst().Run();
        }

        public void OffTimeDown()
        {
            Entities.ForEach((ref TimeDown timeDown, ref NonUniformScale scale) =>
            {
                scale.Value.x = 0f;
            }).WithoutBurst().Run();
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
            int i = 0;
            Entities.ForEach((ref Ball ball, ref Translation position) =>
            {
                if (i != 0 && isTargetBalls[i])
                {
                    if (StaticFuntion.GetPowSizeVector2(positionClosest - positionCueBall) > StaticFuntion.GetPowSizeVector2(position.Value - positionCueBall))
                    {
                        positionClosest = position.Value;
                    }
                }
                i++;
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

        public void SetBallInHand(BallInHand ballInHand)
        {
            this.ballInHand = ballInHand;
        }

        public void SetTargetBalls(int[] serials)
        {
            int i;
            for (i = 0; i < isTargetBalls.Length; i++)
            {
                isTargetBalls[i] = false;
            }
            if (serials != null)
            {
                for (i = 0; i < serials.Length; i++)
                {
                    isTargetBalls[serials[i]] = true;
                }
            }
            direction = GetPositionClosestTargetBall() - positionCueBall;
        }

        public bool ShowMessage(string message)
        {
            if (isShowingMessage)
            {
                return false;
            }
            isShowingMessage = true;
            timeHangOnMessage = 1 + message.Length * 0.1f;
            List<Entity> entitys = new List<Entity>();
            Entities.ForEach((ref Message m, ref Entity e) =>
            {
                entitys.Add(e);
            }).WithoutBurst().Run();
            foreach (Entity entity in entitys)
            {
                TextLayout.SetEntityTextRendererString(EntityManager, entity, message);
            }
            return true;
        }

        public void BotClickAtPosition(float3 position)
        {
            botInput.position = position;
            botInput.isClick = true;
        }

        public void BotChangePower(float scalePower)
        {
            botInput.scalePower = scalePower;
        }

        public void BotShot()
        {
            botInput.isShot = true;
        }

        public bool GetBallOutOfTrack(int serial)
        {
            if (Physics.Instance.IsBallInPocket(serial))
            {
                isCueBallInPocket = true;
                if (!isAnyBallMoving && !Physics.Instance.IsAnyBallRollingInTrack())
                {
                    isCueBallInPocket = false;
                    ballInHand = BallInHand.Free;
                    SetPositionCueBall(float3.zero);
                    Physics.Instance.GetBallOutOfTrack(0);
                    return true;
                }
            }
            return false;
        }

        public bool ActiveInteractive()
        {
            if (IsInteractive && IsTiming && isShoot)
                return false;
            if (!isAnyBallMoving && !isCueBallInPocket)
            {
                isStartTimeDown = true;
                IsInteractive = true;
                IsTimeOut = false;
                serialFirstHit = 0;
                IsHitWrongBall = false;
                isAbleShowGlow = true;
                power = 0;
                return true;
            }
            return false;
        }

        public void OnBall0HitAtBall(int serial)
        {
            if (serialFirstHit == 0)
            {
                serialFirstHit = serial;
                if (!isTargetBalls[serial])
                {
                    IsHitWrongBall = true;
                }
            }
        }
    }
}


public struct BotInput
{
    public float3 position;
    public bool isClick;
    public float scalePower;
    public bool isShot;
}
