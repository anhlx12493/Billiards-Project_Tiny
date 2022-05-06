using FixMath.NET;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using BVector3 = BEPUutilities.Vector3;

namespace Billiards
{
    public class MatchController : SystemBase
    {
        private static readonly Fix64 SIZE_UNIT_SCALE = 0.055m;// 1 / 16.76m;
        private static Fix64 SIMULATION_TIME_SCALE = 1.2m;

        private Match match = new Match();
        private readonly Dictionary<int, BodyDescription> bodyDescriptions
            = new Dictionary<int, BodyDescription>();
        private readonly Dictionary<int, KinematicDescription> kinematicDescriptions
            = new Dictionary<int, KinematicDescription>();
        private readonly Dictionary<int, MeshDescription> meshDescriptions =
            new Dictionary<int, MeshDescription>();
        private readonly Dictionary<int, BodyDescription> lagBodyDescriptions
            = new Dictionary<int, BodyDescription>();

        protected override void OnUpdate()
        {

        }

        public void InitMatch()
        {
            bodyDescriptions.OrderBy(x => x.Key);
            kinematicDescriptions.OrderBy(x => x.Key);
            meshDescriptions.OrderBy(x => x.Key);
            lagBodyDescriptions.OrderBy(x => x.Key);

            SimulationDescription simulationDesc;
            simulationDesc.Gravity = new BVector3(0, 0, 981 * SIZE_UNIT_SCALE);
            simulationDesc.TimeStepDuration = 1 / 360m;
            simulationDesc.VelocityLowerLimit = 1.25m * SIZE_UNIT_SCALE;
            simulationDesc.Bodies = bodyDescriptions.Values.ToArray();
            simulationDesc.Kinematics = kinematicDescriptions.Values.ToArray();
            simulationDesc.Meshes = meshDescriptions.Values.ToArray();

            match.Reinitialize(simulationDesc);

            //if (GameManager.Instance.LagShot)
            //{
            //    LagTable.InitBodies(match.Simulation.Bodies);
            //}
            //else
            //{
            //    Table.InitBodies(match.Simulation.Bodies);
            //}
        }
    }
}
