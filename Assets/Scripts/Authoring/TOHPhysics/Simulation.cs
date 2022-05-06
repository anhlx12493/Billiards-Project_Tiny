using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionTests.CollisionAlgorithms;
using BEPUphysics.CollisionTests.CollisionAlgorithms.GJK;
using BEPUphysics.Entities;
using BEPUphysics.Materials;
using BEPUphysics.NarrowPhaseSystems;
using BEPUphysics.Settings;
using BEPUutilities;
using FixMath.NET;
using System;

namespace Billiards
{
    public class Simulation : IDisposable
    {
        public Space Space { get; private set; }
        public bool Active => Space != null;
        public Entity[] Bodies { get; private set; }
        public Entity[] Kinematics { get; private set; }
        public InstancedMesh[] Meshes { get; private set; }

        static Simulation()
        {
            // In bepuphysics1, unfortunately the material blender is a statically defined thing,
            // so we define it once ahead of time.
            MaterialManager.MaterialBlender = (Material a, Material b, out InteractionProperties properties) =>
            {
                // Conveniently, all material interactions in the demo can be expressed
                // by taking the maximum of friction coefficients
                properties.KineticFriction = MathHelper.Max(a.KineticFriction, b.KineticFriction);
                properties.StaticFriction = MathHelper.Max(a.StaticFriction, b.StaticFriction);
                // And then bounciness is determined by the *least* bouncy material
                properties.Bounciness = MathHelper.Min(a.Bounciness, b.Bounciness);
                // Of course, you can do any kind of blending you want here,
                // but it happens to work out for the set of materials the demo cares about

                /*Debug.Log("KineticFriction = " + properties.KineticFriction);
                Debug.Log("StaticFriction = " + properties.StaticFriction);
                Debug.Log("Bounciness = " + properties.Bounciness);*/
            };

            // Fix low fps at first Space.Update()
            NarrowPhaseHelper.Factories.BoxSphere.EnsureCount(128);
            NarrowPhaseHelper.Factories.SphereSphere.EnsureCount(128);
            //NarrowPhaseHelper.Factories.InstancedMeshSphere.EnsureCount(128);
        }

        /// <summary>
        /// Applies demo appropriate configuration and scales the configuration settings for collision detection and response to handle a different scale interpretation. 
        /// The default configuration (a scale of 1) works well for sizes from about 0.5 to 10, and gracefully degrades outside of that.
        /// For example, if you want to increase your gravity to -100 from -10 and consider a 5 unit wide box to be tiny,
        /// apply a scale of 10 to get the collision response and detection systems to match expectations.
        /// </summary>
        /// <param name="scale">Scale to apply to relevant configuration settings.</param>
        public static void ApplyConfiguration(Fix64 scale)
        {
            //Note that all these properties are static, so there's no Space reference or need to do it on reinitialize.
            //It's something you'd want to call when setting up the simulation initially, set according to the size of stuff in the simulation.
            //This is mostly from the BEPUphysicsDemos ConfigurationHelper.
            CollisionResponseSettings.MaximumPenetrationRecoverySpeed = 2m * scale;
            //Note that bounciness threshold is an extremely important value for any simulation with high bounciness, velocities, and especially gravity, which this billiards demo has.
            //If the value is too low, bodies will jitter on surfaces rather than settling down. You'll tend to see bodies slowly rolling or otherwise behaving very weirdly.
            //The default is 1 * scale, but empirically, a higher value is useful. 
            //Going too high will stop the balls from bouncing from things when they should.
            //Note that this is sensitive to time step duration.
            //If you're using longer time steps, you will likely need to increase this to stop balls from jittering due to the high gravity relative to ball size.
            //From some brief testing, for a given time step duration, you'll probably want the something around these values for bounciness threshold:
            //1/60: 12 * scale
            //1/120: 6 * scale
            //1/240: 2 * scale
            //1/480: 1.5 * scale
            //If you can afford it computationally, use shorter time steps to get better quality.
            CollisionResponseSettings.BouncinessVelocityThreshold = 1.5m * scale;
            CollisionResponseSettings.StaticFrictionVelocityThreshold = .2m * scale;
            //Softness is useful for improving stability in stacks, but doesn't help much for billiards. 
            //High softness can mess with bounciness, damping out energy that should have gone back into the ball's movement. So we just set it to 0.
            CollisionResponseSettings.Softness = 0; //.001m
            CollisionDetectionSettings.ContactInvalidationLength = 0.1m * scale;
            //For the purposes of this demo, we only ever want one contact since all bodies are spheres.
            //Keeping older contacts can actually be a problem at the scales/speeds involved; it can cause rolling artifacts if penetration depth is high.
            //So we just use a larger contact separation minimum distance so old contacts get forcibly updated by new contact entries.
            //(Plus, getting rid of old contacts just means there are fewer constraints to solve!)
            CollisionDetectionSettings.ContactMinimumSeparationDistance = 0.1m * scale; //0.03m * scale;
            CollisionDetectionSettings.MaximumContactDistance = .1m * scale;
            CollisionDetectionSettings.DefaultMargin = .04m * scale;
            CollisionDetectionSettings.AllowedPenetration = .01m * scale;
            Toolbox.Epsilon = 1e-7m * scale;
            Toolbox.BigEpsilon = 1e-5m * scale;
            MPRToolbox.DepthRefinementEpsilon = 1e-4m * scale;
            MPRToolbox.RayCastSurfaceEpsilon = 1e-9m * scale;
            MPRToolbox.SurfaceEpsilon = 1e-7m * scale;
            PairSimplex.DistanceConvergenceEpsilon = 1e-7m * scale;
            PairSimplex.ProgressionEpsilon = 1e-8m * scale;
            //For the demo, we fully disable constraint early outs. Helps with consistency in corner cases.
            //This could be scaled too, if it had a nonzero value.
            //SolverSettings.DefaultMinimumImpulse = 0;
            //This leaves out space-specific settings, like space.DeactivationManager.VelocityLowerLimit.
            //That's fine in context- this demo disables deactivation stabilization completely in favor of a custom damping curve.
        }

        /// <summary>
        /// Modifies a body description's velocities as if a highly nonphysical cue stick impacted the body with the described properties.
        /// </summary>
        /// <param name="bodyDesc">Body description to modify.</param>
        /// <param name="cueShot">Cue shot to apply to the body state.</param>
        public static void ApplyCueShotToBodyDescription(ref BodyDescription bodyDesc, CueShot cueShot)
        {
            //If you want to embrace a nonphysical impact model, you could do something like this.
            //It's a hack, but if it gets you closer to where you want, that's fine.
            bodyDesc.LinearVelocity = cueShot.ImpactDirection * cueShot.ImpactVelocity;
            //There is no 'correct' way to handle linear versus angular contribution here,
            //so an arbitrary scaling factor is applied to the angular component.
            bodyDesc.AngularVelocity = cueShot.AngularScale * Vector3.Cross(cueShot.Offset,
                cueShot.ImpactDirection * cueShot.ImpactVelocity);
        }

        /// <summary>
        /// Rebuilds the Space for the given simulation description.
        /// </summary>
        /// <param name="desc">Description to use to rebuild the Space.</param>
        public void Reinitialize(SimulationDescription desc)
        {
            if (Space != null)
            {
                Clear();
            }

            //Note that this is the only place where a Space is actually constructed.
            //Creating a Space is a determinism sensitive process; if it occurs in a different order, it can cause the simulation to produce different results from identical initial states.
            //This demo recreates the simulation as needed to guarantee the Space's internal state is consistent, provided a set of initial states.
            //(In other words, regardless of how those initial states were found or provided, creating a new Space for them from scratch screens off all the possible sources of nondeterminism.)
            Space = new Space();
            Space.ForceUpdater.Gravity = desc.Gravity;
            Space.TimeStepSettings.TimeStepDuration = desc.TimeStepDuration;
            //A low iteration limit keeps things speedy.
            Space.Solver.IterationLimit = 2;
            //IMPORTANT:
            //Stabilization applies strong damping to bodies with low velocities (below Space.DeactivationManager.VelocityLowerLimit).
            //For the purposes of this demo, we want to show how to explicitly control damping, so we'll disable this feature entirely.
            Space.DeactivationManager.UseStabilization = false;
            //Even with stabilization disabled, the velocity lower limit is still important for deactivation.
            //Deactivation is the condition we use to decide whether a turn is done, so make sure it's got a reasonable value.
            Space.DeactivationManager.VelocityLowerLimit = desc.VelocityLowerLimit;
            //This defaults to 1, but lower values can make sleeping more aggressive.
            //That can be a problem in some simulations, but billiards should be fairly forgiving.
            Space.DeactivationManager.LowVelocityTimeMinimum = 0.2m;
            Bodies = new Entity[desc.Bodies.Length];
            //Material properties are really a product of both involved materials. We include some baselines in the material definitions to work with,
            //but the real values in a given collision are computed by the material blender function.
            //Note that the cue shot has its own coefficient of friction defined within ApplyCueShotToBodyDescription (since the cue is not a real body in the physics engine).
            //The cue impact usually has a significantly higher coefficient of friction due to the material and chalk.
            //Note that rolling friction is handled in the TimeStep function as a part of the custom damping behavior.
            //These values are taken from empirical measurements from some google results. Feel free to play with them or make them configurable on a per-body basis.
            //var ballMaterial = new Material(0.06m, 0.06m, 0.93m);
            //var tableMaterial = new Material(0.2m, 0.2m, 0.5m);
            //var railMaterial = new Material(0.2m, 0.2m, 0.8m);

            for (int i = 0; i < desc.Bodies.Length; ++i)
            {
                ref var bodyDesc = ref desc.Bodies[i];

                Bodies[i] = new Entity(bodyDesc.Shape, bodyDesc.Mass, bodyDesc.LocalInertia)
                {
                    Position = bodyDesc.Position,
                    Orientation = bodyDesc.Orientation,
                    LinearVelocity = bodyDesc.LinearVelocity,
                    AngularVelocity = bodyDesc.AngularVelocity,
                    //Note that this demo uses an explicit damping implementation that gives arbitrary control over damping behavior.
                    //We only use linear damping here to simulate a tiny bit of air resistance.
                    //It's almost irrelevant compared to rolling friction.
                    LinearDamping = 0.001m,
                    AngularDamping = 0,
                    Material = bodyDesc.Material
                };
                //While the simulation should be run with a fast time step,
                //the velocities involved in a really hard shot could be enough to shove a ball straight through a wall.
                //That's pretty nasty, so use continuous collision detection for all balls.
                Bodies[i].PositionUpdateMode = BEPUphysics.PositionUpdating.PositionUpdateMode.Continuous;
                Bodies[i].CollisionInformation.Tag = bodyDesc.Tag;
                Space.Add(Bodies[i]);
            }

            //Note that all kinematics are added after all dynamics,
            //so iterating over dynamics is simply iterating over 0 to Bodies.Length.
            Kinematics = new Entity[desc.Kinematics.Length];

            for (int i = 0; i < desc.Kinematics.Length; ++i)
            {
                ref var kinematicDesc = ref desc.Kinematics[i];

                Kinematics[i] = new Entity(kinematicDesc.Shape)
                {
                    Position = kinematicDesc.Position,
                    Orientation = kinematicDesc.Orientation,
                    Material = kinematicDesc.Material,
                };

                if (kinematicDesc.IsTrigger)
                {
                    Kinematics[i].CollisionInformation.CollisionRules.Personal =
                        BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver;
                }

                Kinematics[i].CollisionInformation.Tag = kinematicDesc.Tag;
                Space.Add(Kinematics[i]);
            }

            Meshes = new InstancedMesh[desc.Meshes.Length];

            for (int i = 0; i < desc.Meshes.Length; ++i)
            {
                ref var meshDesc = ref desc.Meshes[i];

                Meshes[i] = new InstancedMesh(meshDesc.Shape, meshDesc.Transform)
                {
                    Material = meshDesc.Material
                };

                Meshes[i].Tag = meshDesc.Tag;
                Space.Add(Meshes[i]);
            }
        }

        /// <summary>
        /// Apply a body state to the simulation.
        /// </summary>
        /// <param name="bodyIndex">Index of the body to apply the description to.</param>
        /// <param name="bodyDesc">Description to apply.</param>
        public void ApplyBodyDescription(int bodyIndex, BodyDescription bodyDesc)
        {
            if (Space == null)
            {
                throw new InvalidOperationException("Cannot apply a state to a simulation that is inactive.");
            }

            if (bodyIndex < 0 || bodyIndex >= Space.Entities.Count)
            {
                throw new ArgumentException($"Body index must be from 0 and {Space.Entities.Count}.");
            }

            var target = Space.Entities[bodyIndex];
            target.Position = bodyDesc.Position;
            target.Orientation = bodyDesc.Orientation;
            target.LinearVelocity = bodyDesc.LinearVelocity;
            target.AngularVelocity = bodyDesc.AngularVelocity;
            target.Mass = bodyDesc.Mass;
            target.LocalInertiaTensor = bodyDesc.LocalInertia;
        }

        /// <summary>
        /// Performs any necessary cleanup for objects within the simulation. After being cleared, it can be reused with the Reinitialize function.
        /// </summary>
        public void Clear()
        {
            if (Space == null)
            {
                throw new InvalidOperationException("Simulation is is not in an active state; cannot reclear.");
            }

            //If collidables keep getting recreated from the same shapes over and over,
            //it'll gradually fill up the ShapeChanged event and cause a memory leak.
            //Unhooking the shape changed event removes the event registration.
            for (int i = 0; i < Bodies.Length; ++i)
            {
                Bodies[i].CollisionInformation.ShapeChangedHooked = false;
                Bodies[i].IgnoreShapeChanges = true;
            }

            for (int i = 0; i < Meshes.Length; ++i)
            {
                Meshes[i].ShapeChangedHooked = false;
            }

            //Drop the space reference. This serves as a flag that this simulation has been cleared for the Reinitialize function.
            Space = null;
        }

        /// <summary>
        /// Records body poses from the simulation into a provided span.
        /// </summary>
        /// <param name="poses">Span of poses to hold the body states.</param>
        public void WritePoses(BodyPose[] poses)
        {
            if (poses.Length != Space.Entities.Count)
            {
                throw new ArgumentException("Span length should match the number of bodies in the simulation.");
            }

            for (int i = 0; i < poses.Length; ++i)
            {
                poses[i].Position = Space.Entities[i].Position;
                poses[i].Orientation = Space.Entities[i].Orientation;
            }
        }

        /// <summary>
        /// Records body states from the simulation into a provided span.
        /// </summary>
        /// <param name="descs">Span of body descriptions to hold the body states.</param>
        public void WriteBodyDescriptions(BodyDescription[] descs)
        {
            if (descs.Length != Bodies.Length)
            {
                throw new ArgumentException("Span length should match the number of bodies in the simulation.");
            }

            for (int i = 0; i < descs.Length; ++i)
            {
                var source = Space.Entities[i];

                descs[i] = new BodyDescription
                {
                    Position = source.Position,
                    Orientation = source.Orientation,
                    LinearVelocity = source.LinearVelocity,
                    AngularVelocity = source.AngularVelocity,
                    Mass = source.Mass,
                    LocalInertia = source.LocalInertiaTensor,
                    Shape = source.CollisionInformation.Shape
                };
            }
        }

        public void Dispose()
        {
            if (Space != null)
            {
                Clear();
            }
        }

        public void TimeStep()
        {
            if (!Active)
            {
                throw new InvalidOperationException("Simulation is not in an active state; cannot simulate.");
            }

            Space.Update();

            for (int i = 0; i < Bodies.Length; ++i)
            {
                //Note that we iterate only over the dynamic bodies in the space,
                //and that we guarantee that any kinematic bodies are stored after the dynamic bodies.
                //So we can use a simple index here.
                var body = Space.Entities[i];

                if (body.IsDynamic && body.ActivityInformation.IsActive)
                {
                    //In this demo, the DeactivationManager stabilization feature are disabled and damping is near zero.
                    //You could use them if you'd like, but I wanted to show an alternative approach
                    //that's a little closer to how billiards works.
                    //The dominant source of deceleration for billiard balls is rolling friction against the cloth table.
                    //There can only be rolling friction if the ball is actually touching a table, so check that first.
                    //The more strongly a ball is shoved into a cloth surface, the more friction we'll apply.
                    Fix64 impulseSum = 0;
                    //Spheres are rotationally symmetrical, so all entries along the diagonal are the same.
                    //We can use that directly as the 'effective mass' of twist friction since we know it won't vary with orientation.
                    var ballDiagonalInertia = body.LocalInertiaTensor.M11;
                    Fix64 coefficientOfTwistFriction = 0.01m;

                    foreach (var pair in body.CollisionInformation.Pairs)
                    {
                        if (pair.Contacts.Count > 0)
                        {
                            var other = pair.CollidableA == body.CollisionInformation ? pair.CollidableB : pair.CollidableA;
                            //Check if the thing we're hitting is part of the environment.
                            //In the demo, we treat any kinematic entities or non-entity collidables as environmental.
                            //Note that you could put information into the Tag property of the collidable
                            //to do other kinds of tests or game logic.
                            //(Also note that Entity.Tag is a different field from Entity.CollisionInformation.Tag!
                            //Since we're handling a collidable here, you'd probably want
                            //to use the Entity.CollisionInformation.Tag to store information used by this test.)
                            if (!(other is EntityCollidable otherEntityCollidable)
                                || otherEntityCollidable.Entity.InverseMass == 0)
                            {
                                //We're in contact with a mesh or other environmental object.
                                foreach (var contact in pair.Contacts)
                                {
                                    impulseSum += contact.NormalImpulse;
                                    //Fight twisting motion around the contact's normal. Balls don't spin like a top forever in reality!
                                    //Our simulation level angular damping is 0, so this is important.
                                    var velocityAroundNormal = Vector3.Dot(contact.Contact.Normal, body.AngularVelocity);

                                    if (velocityAroundNormal != 0m)
                                    {
                                        var twistFrictionImpulse = velocityAroundNormal * ballDiagonalInertia;
                                        //The friction impulse targets zero velocity, but is limited by the normal impulse
                                        //and coefficient of twist friction.
                                        var maximumTwistFrictionImpulseMagnitude = coefficientOfTwistFriction * contact.NormalImpulse;
                                        if (twistFrictionImpulse > maximumTwistFrictionImpulseMagnitude)
                                        {
                                            twistFrictionImpulse = maximumTwistFrictionImpulseMagnitude;
                                        }
                                        else if (twistFrictionImpulse < -maximumTwistFrictionImpulseMagnitude)
                                        {
                                            twistFrictionImpulse = -maximumTwistFrictionImpulseMagnitude;
                                        }
                                        var angularImpulse = contact.Contact.Normal * -twistFrictionImpulse;
                                        //Note that we use ApplyAngularImpulse instead of modifying the velocity directly.
                                        //Modifying the velocity property of an entity in bepuphysics1 stops it from going to sleep,
                                        //and this function explicitly does not.
                                        body.ApplyAngularImpulse(ref angularImpulse);
                                    }
                                }
                            }
                        }
                    }

                    if (impulseSum > 0)
                    {
                        //The rolling resistance is a dimensionless coefficient that maps normal force to frictional force.
                        //The more force pushes down, the larger the resulting force will be.
                        //We're being a bit loosey-goosey here and applying the rolling friction directly
                        //to the linear velocity rather than working it out on a per-contact basis.
                        Fix64 rollingResistance = StaticDataPhysics.RollingResistance;
                        //Fix64 rollingResistance = 0.01m;
                        var maximumRollingFrictionForce = impulseSum * rollingResistance;
                        var velocityLengthSquared = body.LinearVelocity.LengthSquared();

                        if (velocityLengthSquared > 1e-15m)
                        {
                            var velocityMagnitude = Fix64.Sqrt(velocityLengthSquared);
                            var velocityDirection = body.LinearVelocity / velocityMagnitude;
                            var impulseToReachZeroVelocity = velocityMagnitude * body.Mass;
                            //Note that it won't try to decelerate below zero magnitude!
                            //That's important to stop velocity from oscillating around zero.
                            var rollingFrictionImpulse = velocityDirection
                                * -(impulseToReachZeroVelocity > maximumRollingFrictionForce
                                ? maximumRollingFrictionForce : impulseToReachZeroVelocity);
                            //Note that we use ApplyLinearImpulse instead of modifying the velocity directly.
                            //Modifying the velocity property of an entity in bepuphysics1 stops it from going to sleep,
                            //and this function explicitly does not.
                            body.ApplyLinearImpulse(ref rollingFrictionImpulse);
                        }
                    }
                }

                if (body.LinearVelocity.Z < 0) // clamped to not go out of table
                {
                    body.linearVelocity.Z = 0;
                }
            }
        }
    }
}