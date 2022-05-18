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
                        script++;
                    }
                    break;
                case Script.switch_to_bot_turn:
                    if (GameController.Instance.ShowMessage("Bot turn"))
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
                            GameController.Instance.BotClickAtPosition(GetPositionBot());
                            step++;
                            time = 0;
                            break;
                        case 4:
                            if (time < 1f)
                                time += Time.DeltaTime;
                            else
                            {
                                step++;
                                time = 0;
                            }
                            break;
                        case 5:
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
                    }
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
    }
}
