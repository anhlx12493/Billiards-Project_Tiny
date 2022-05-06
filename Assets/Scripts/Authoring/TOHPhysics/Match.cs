using FixMath.NET;
using System;

namespace Billiards
{
    public enum GameState
    {
        WaitingForShot,
        Simulating
    }

    public class Match : IDisposable
    {
        private SimulationDescription simulationDescription;
        private CueShot pendingCueShot;
        private CueShot previousCueShot;

        public Simulation Simulation { get; private set; } = new Simulation();

        public Fix64 TimeStepDuration => simulationDescription.TimeStepDuration;

        public GameState GameState { get; private set; }

        public void Reinitialize(SimulationDescription desc)
        {
            Simulation.Reinitialize(desc);
            simulationDescription = SimulationDescription.CreateCopy(desc);
            GameState = GameState.WaitingForShot;
            pendingCueShot.Reset();
            previousCueShot.Reset();
        }

        public void Shoot(CueShot shot)
        {
            if (shot.BodyIndex >= 0 && shot.BodyIndex < simulationDescription.Bodies.Length)
            {
                Simulation.WriteBodyDescriptions(simulationDescription.Bodies);
                Simulation.ApplyCueShotToBodyDescription(
                    ref simulationDescription.Bodies[shot.BodyIndex], shot);

                Simulation.ApplyBodyDescription(shot.BodyIndex,
                    simulationDescription.Bodies[shot.BodyIndex]);

                GameState = GameState.Simulating;
            }
        }

        public void Update()
        {
            if (GameState == GameState.Simulating)
            {
                var allBodiesAsleep = true;

                for (int i = 0; i < Simulation.Bodies.Length; ++i)
                {
                    if (Simulation.Space.Entities[i].ActivityInformation.IsActive)
                    {
                        allBodiesAsleep = false;
                        break;
                    }
                }

                if (allBodiesAsleep)
                {
                    GameState = GameState.WaitingForShot;
                }
                else
                {
                    Simulation.TimeStep();
                }
            }
            else
            {
                if (!pendingCueShot.Equals(previousCueShot) && pendingCueShot.BodyIndex > -1)
                {
                    Simulation.WriteBodyDescriptions(simulationDescription.Bodies);
                    Simulation.ApplyCueShotToBodyDescription(
                        ref simulationDescription.Bodies[pendingCueShot.BodyIndex],
                        pendingCueShot);

                    previousCueShot = pendingCueShot;
                }

                if (pendingCueShot.BodyIndex > -1 && pendingCueShot.ImpactVelocity > 0)
                {
                    Simulation.ApplyBodyDescription(pendingCueShot.BodyIndex,
                        simulationDescription.Bodies[pendingCueShot.BodyIndex]);
                    pendingCueShot.Reset();
                    previousCueShot.Reset();
                    GameState = GameState.Simulating;
                }
            }
        }

        public void Dispose()
        {
            Simulation.Dispose();
        }
    }
}