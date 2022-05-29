using Unity.Entities;
using Unity.Mathematics;
using Unity.Tiny;
using Unity.Tiny.Animation;
using Unity.Transforms;

namespace Billiards 
{
    public class ExcuteScript : SystemBase
    {
        public enum Script { start, switch_to_bot_turn, bot_play, switch_to_your_turn, wait_result, show_result }

        private Script script;

        int step;
        float time;

        protected override void OnUpdate()
        {
            if (GameController.Instance != null)
            {
                if (step == 0)
                {
                    GameController.Instance.SetBallInHand(GameController.BallInHand.Free);
                    int[] targetBalls = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
                    GameController.Instance.SetTargetBalls(targetBalls);
                    GameController.Instance.isPlayer = true;
                    GameController.Instance.ActiveInteractive();
                }
                if (step % 100 == 0)
                {
                    GameController.Instance.isPlayer = true;
                    GameController.Instance.ActiveInteractive();
                }

                step++;
            }
            return;
            switch (script)
            {
                case Script.start:
                    if (GameController.Instance != null)
                    {
                        GameController.Instance.ActiveInteractive();
                        GameController.Instance.isPlayer = false;
                        GameController.Instance.SetBallInHand(GameController.BallInHand.none);
                        GameController.Instance.isPlayer = false;
                        int[] targetBalls = { 2, 3 };
                        GameController.Instance.SetTargetBalls(targetBalls);
                        script++;
                    }
                    break;
                case Script.switch_to_bot_turn:
                    if (GameController.Instance.ShowMessage("Bot's turn"))
                    {
                        script++;
                        step = 0;
                    }
                    break;
                case Script.bot_play:
                    switch (step)
                    {
                        case 0:
                            if (time < 1f)
                                time += Time.DeltaTime;
                            else
                            {
                                step++;
                                time = 0;
                            }
                            break;
                        case 1:
                            GameController.Instance.BotClickAtPosition(Physics.Instance.GetPositionBall(2));
                            step++;
                            time = 0;
                            break;
                        case 2:
                            if (time < 2f)
                                time += Time.DeltaTime;
                            else
                                step++;
                            break;
                        case 3:
                            GameController.Instance.BotClickAtPosition(Physics.Instance.GetPositionBall(3));
                            step++;
                            time = 0;
                            break;
                        case 4:
                            if (time < 1f)
                                time += Time.DeltaTime;
                            else
                                step++;
                            break;
                        case 5:
                            GameController.Instance.BotClickAtPosition(GetPositionBot());
                            step++;
                            time = 0;
                            break;
                        case 6:
                            if (time < 1f)
                                time += Time.DeltaTime;
                            else
                            {
                                step++;
                                time = 0;
                            }
                            break;
                        case 7:
                            if (time < 1f)
                            {
                                time += Time.DeltaTime;
                                if (time > 1)
                                    time = 1;
                                GameController.Instance.BotChangePower(time * GetBot().scalePower);
                            }
                            else
                            {
                                GameController.Instance.BotShot();
                                step++;
                                time = 0;
                            }
                            break;
                        case 8:
                            if (Physics.Instance.IsAnyBallMoving)
                            {
                                step++;
                            }
                            break;
                        case 9:
                            if (!Physics.Instance.IsAnyBallMoving)
                            {
                                script++;
                                step = 0;
                            }
                            break;
                    }
                    break;
                case Script.switch_to_your_turn:
                    switch (step)
                    {
                        case 0:
                            if (GameController.Instance.ShowMessage("Your turn"))
                            {
                                GameController.Instance.isPlayer = true;
                                int[] targetBalls = { 1 };
                                GameController.Instance.SetTargetBalls(targetBalls);
                                step++;
                            }
                            break;
                        case 1:
                            if (GameController.Instance.ActiveInteractive())
                            {
                                script++;
                                step = 0;
                            }
                            break;

                    }
                    break; 
                case Script.wait_result:
                    switch (step)
                    {
                        case 0:
                            if (GameController.Instance.IsTimeOut)
                            {
                                GameController.Instance.isPlayer = false;
                                int[] targetBalls = { 2, 3 };
                                GameController.Instance.SetTargetBalls(targetBalls);
                                script++;
                                break;
                            }
                            if (Physics.Instance.IsAnyBallMoving)
                            {
                                step++;
                            }
                            break;
                        case 1:
                            if (Physics.Instance.IsBallInPocket(2))
                            {
                                DisableTargetInfoBall(0);
                            }
                            if (Physics.Instance.IsBallInPocket(3))
                            {
                                DisableTargetInfoBall(1);
                            }
                            if (!Physics.Instance.IsAnyBallMoving && !Physics.Instance.IsAnyBallRollingInTrack())
                            {
                                GameController.Instance.isPlayer = false;
                                int[] targetBalls = { 2, 3 };
                                GameController.Instance.SetTargetBalls(targetBalls);
                                step++;
                            }
                            break;
                        case 2:
                            if (time < 1f)
                                time += Time.DeltaTime;
                            else
                            {
                                script++;
                                step = 0;
                            }
                            break;
                    }
                    break;
                case Script.show_result:
                    GameController.Instance.OffTimeDown();
                    if (Physics.Instance.IsBallInPocket(1) && !Physics.Instance.IsBallInPocket(0) && !GameController.Instance.IsHitWrongBall)
                    {
                        ActivePopupWin();
                    }
                    else
                    {
                        ActivePopupLose();
                    }
                    script++;
                    break;
            }
        }

        private BotManager GetBot()
        {
            BotManager bot = new BotManager();
            Entities.ForEach((ref BotManager b) =>
            {
                bot = b;
            }).WithoutBurst().Run();
            return bot;
        }

        private float3 GetPositionBot()
        {
            float3 pos = 0;
            Entities.ForEach((ref BotManager b, ref Translation position) =>
            {
                pos = position.Value;
            }).WithoutBurst().Run();
            return pos;
        }

        private void ActivePopupWin()
        {
            Entity entity = Entity.Null;
            Entities.ForEach((ref Popup popup, ref Entity e, ref Translation p) =>
            {
                if (popup.subject == Popup.Subject.win)
                {
                    entity = e;
                    p.Value.y = 4;
                }
            }).WithoutBurst().Run();
            if (entity != Entity.Null)
            {
                TinyAnimation.SetTime(World, entity,0);
                TinyAnimation.Play(World, entity);
            }
        }

        private void ActivePopupLose()
        {
            Entity entity = Entity.Null;
            Entities.ForEach((ref Popup popup, ref Entity e, ref Translation p) =>
            {
                if (popup.subject == Popup.Subject.lose)
                {
                    entity = e;
                    p.Value.y = 4;
                }
            }).WithoutBurst().Run();
            if (entity != Entity.Null)
            {
                TinyAnimation.SetTime(World, entity, 0);
                TinyAnimation.Play(World, entity);
            }
        }

        private void DisableTargetInfoBall(int serial)
        {
            Entity entity = Entity.Null;
            int i = 0;
            Entities.ForEach((ref InforTargetBall target, ref Translation position) =>
            {
                if (serial == i++)
                {
                    position.Value.y = 1000;
                }
            }).WithoutBurst().Run();
        }
    }
}
