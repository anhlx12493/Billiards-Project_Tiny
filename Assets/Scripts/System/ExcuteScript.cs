using Unity.Entities;
using Unity.Mathematics;
using Unity.Tiny;
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
            switch (script)
            {
                case Script.start:
                    if (GameController.Instance != null)
                    {
                        GameController.Instance.isPlayer = false;
                        GameController.Instance.SetBallInHand(GameController.BallInHand.none);
                        GameController.Instance.BotClickAtPosition(Physics.Instance.GetPositionBall(3));
                        EnableTargetInfoBall(0, true);
                        EnableTargetInfoBall(1, true);
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
                            }
                            break;
                    }
                    break;
                case Script.switch_to_your_turn:
                    if (GameController.Instance.ShowMessage("Your turn"))
                    {
                        GameController.Instance.isPlayer = true;
                        int[] targetBalls = { 1 };
                        GameController.Instance.SetTargetBalls(targetBalls);
                        script++;
                        step = 0;
                    }
                    break;
                case Script.wait_result:
                    switch (step)
                    {
                        case 0:
                            if (Physics.Instance.IsAnyBallMoving)
                            {
                                step++;
                            }
                            break;
                        case 1:
                            if (Physics.Instance.IsBallInPocket(2))
                            {
                                EnableTargetInfoBall(0, false);
                            }
                            if (Physics.Instance.IsBallInPocket(3))
                            {
                                EnableTargetInfoBall(1, false);
                            }
                            if (!Physics.Instance.IsAnyBallMoving)
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
                    if (Physics.Instance.IsBallInPocket(1) && !Physics.Instance.IsBallInPocket(0))
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
            Entities.ForEach((ref GameManager gameManager) =>
            {
                entity = gameManager.popupWin;
            }).WithoutBurst().Run();
            if (entity != Entity.Null)
            {
                EntityManager.SetEnabled(entity, true);
            }
        }

        private void ActivePopupLose()
        {
            Entity entity = Entity.Null;
            Entities.ForEach((ref GameManager gameManager) =>
            {
                entity = gameManager.popupLose;
            }).WithoutBurst().Run();
            if (entity != Entity.Null)
            {
                EntityManager.SetEnabled(entity, true);
            }
        }

        private void EnableTargetInfoBall(int serial, bool isActive)
        {
            Entity entity = Entity.Null;
            Entities.ForEach((ref GameManager gameManager) =>
            {
                switch (serial)
                {
                    case 0:
                        entity = gameManager.targetInfoBall0;
                        break;
                    case 1:
                        entity = gameManager.targetInfoBall1;
                        break;
                }
            }).WithoutBurst().Run();
            if (entity != Entity.Null)
            {
                EntityManager.SetEnabled(entity, isActive);
            }
        }
    }
}
