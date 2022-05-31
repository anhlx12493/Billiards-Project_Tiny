using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Tiny;
using Unity.Tiny.Input;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Billiards
{
    public class Physics : SystemBase
    {
        public static Physics Instance;

        private static float friction;
        private static float frictionSurface;
        private static float radiusBall;
        private static float diameterBall;
        public static float powDiameterBall;
        public bool IsAnyBallMoving { get; private set; }


        private bool isLoadedSetting;

        private float3[] currentPositionBall;
        private float3[] currentVelocityBall;
        private float3[] currentMoveAtCollisionBall;
        private float3[] currentRotationBall;
        private float3[] currentPositionStartOfPathTrackBall;


        private int[] currentExcludeLine;
        private int[] ramdomSerial;
        private int[] timesFallInPocket;
        private int[] currentPathOfTrackBall;

        private int[] softIdByPositionXBallAtSerial;
        private int[] softIdByPositionXBallAtElement;
        private int[] softIdByPositionZBallAtSerial;
        private int[] softIdByPositionZBallAtElement;

        private bool[] isApplyFriction;
        private bool[] isInPocket;
        private bool[] isInTrack;
        private bool[] isFreezeInTrack;

        private Pocket[] inPocket;

        private float3[] BoardLine;
        private float3[] BoardLineNormalInside;
        private float3[] TrackLine;

        private Random random;

        private List<int> listSerialBallInTrack = new List<int>();

        private List<PhysicsEvent> physicsEvents = new List<PhysicsEvent>();

        protected override void OnStartRunning()
        {
            Instance = this;
        }

        protected override void OnUpdate()
        {
            if (!isLoadedSetting)
            {
                LoadSetting();
                InitBalls();
                UpdateCurrentPositionBall();
                InitBoardLine();
                InitTrackLine();
                return;
            }
            MoveAllBall();
        }

        private void InitBoardLine()
        {
            bool isHaveResult;
            int count = 0;
            Entities.ForEach((ref PointCollider point) =>
            {
                count++;
            }).WithoutBurst().Run();
            Debug.Log(count);
            BoardLine = new float3[count];
            BoardLineNormalInside = new float3[count];
            count = 0;
            Entities.ForEach((ref PointCollider point, ref Translation position) =>
            {
                BoardLine[count++] = position.Value;
            }).WithoutBurst().Run();
            float3 vector;
            float sp;
            float3[] BoardLineCopy = new float3[BoardLine.Length];
            for (count = 0; count < BoardLine.Length - 1; count++)
            {
                vector = BoardLine[count] - BoardLine[count + 1];
                sp = vector.x;
                vector.x = -vector.z;
                vector.z = sp;
                ResizeVector2(ref vector, radiusBall);
                BoardLineCopy[count] = BoardLine[count];
                BoardLine[count] += vector;
                BoardLineNormalInside[count] = vector;
            }
            vector = BoardLine[BoardLine.Length - 1] - BoardLine[0];
            sp = vector.x;
            vector.x = -vector.z;
            vector.z = sp;
            ResizeVector2(ref vector, radiusBall);
            BoardLineCopy[BoardLine.Length - 1] = BoardLine[BoardLine.Length - 1];
            BoardLine[BoardLine.Length - 1] += vector;
            BoardLineNormalInside[BoardLine.Length - 1] = vector;

            vector = BoardLineCopy[0] - BoardLineCopy[BoardLine.Length - 1];
            sp = vector.x;
            vector.x = -vector.z;
            vector.z = sp;
            float a1 = vector.x;
            float b1 = vector.z;
            float c1 = -a1 * BoardLine[BoardLine.Length - 1].x - b1 * BoardLine[BoardLine.Length - 1].z;
            vector = BoardLineCopy[1] - BoardLineCopy[0];
            sp = vector.x;
            vector.x = -vector.z;
            vector.z = sp;
            float a2 = vector.x;
            float b2 = vector.z;
            float c2 = -a2 * BoardLine[0].x - b2 * BoardLine[0].z;
            BoardLine[0] = GetIntersectionPosition(a1, b1, c1, a2, b2, c2, out isHaveResult);
            a1 = a2;
            b1 = b2;
            c1 = c2;
            for (count = 1; count < BoardLine.Length - 1; count++)
            {
                vector = BoardLineCopy[count + 1] - BoardLineCopy[count];
                sp = vector.x;
                vector.x = -vector.z;
                vector.z = sp;
                a2 = vector.x;
                b2 = vector.z;
                c2 = -a2 * BoardLine[count].x - b2 * BoardLine[count].z;
                BoardLine[count] = GetIntersectionPosition(a1, b1, c1, a2, b2, c2, out isHaveResult);
                a1 = a2;
                b1 = b2;
                c1 = c2;
            }
            vector = BoardLineCopy[0] - BoardLineCopy[count];
            sp = vector.x;
            vector.x = -vector.z;
            vector.z = sp;
            a2 = vector.x;
            b2 = vector.z;
            c2 = -a2 * BoardLine[count].x - b2 * BoardLine[count].z;
            BoardLine[count] = GetIntersectionPosition(a1, b1, c1, a2, b2, c2, out isHaveResult);
        }

        private void InitTrackLine()
        {
            int count = 0;
            Entities.ForEach((ref TrackPointLine point) =>
            {
                count++;
            }).WithoutBurst().Run();
            TrackLine = new float3[count];
            count = 0;
            Entities.ForEach((ref TrackPointLine point, ref Translation position) =>
            {
                TrackLine[count++] = position.Value;
            }).WithoutBurst().Run();
        }

        private void InitBalls()
        {
            int count = 0;
            Entities.ForEach((ref Ball ball) =>
            {
                count++;
            }).WithoutBurst().Run();
            if (count == 16)
            {
                int i = 0;
                float3 putBall = new float3(1.5f, 0, 0);
                float distanPutX = math.sin(math.PI / 3) * radiusBall * 2.01f;
                int rowPut = 0;
                int column = 0;
                int maxCollumn = 1;
                float topColumnPos = 0;
                Entities.ForEach((ref Ball ball, ref Translation position) =>
                {
                    if (i > 0)
                    {
                        putBall.x = 1.5f + rowPut * distanPutX;
                        putBall.z = topColumnPos + column * radiusBall * 2.01f;
                        position.Value = putBall;
                        column++;
                        if (column == maxCollumn)
                        {
                            maxCollumn++;
                            column = 0;
                            rowPut++;
                            topColumnPos -= radiusBall * 1.005f;
                        }
                    }
                    i++;
                }).WithoutBurst().Run();
            }
            random = new Random(1);
            currentPositionBall = new float3[count];
            currentVelocityBall = new float3[count];
            currentRotationBall = new float3[count];
            currentMoveAtCollisionBall = new float3[count];
            currentPositionStartOfPathTrackBall = new float3[count];
            isInPocket = new bool[count];
            isInTrack = new bool[count];
            isApplyFriction = new bool[count];
            isFreezeInTrack = new bool[count];
            inPocket = new Pocket[count];
            currentExcludeLine = new int[count];
            ramdomSerial = new int[count];
            currentPathOfTrackBall = new int[count];
            timesFallInPocket = new int[count];
            for (int i = 0; i < count; i++)
            {
                currentExcludeLine[i] = -1;
                ramdomSerial[i] = i;
            }
            UpdateRandomSerial(count);
        }

        private void LoadSetting()
        {
            Entities.ForEach((ref PhysicsSetting setting) =>
            {
                isLoadedSetting = true;

                friction = setting.friction;
                frictionSurface = setting.frictionSurface;
                radiusBall = setting.radiusBall;
                diameterBall = radiusBall * 2;
                powDiameterBall = math.pow(diameterBall, 2);
            }).WithoutBurst().Run();
        }

        private void UpdateCurrentPositionBall()
        {
            int i = 0;
            Entities.ForEach((ref Ball ball, ref Translation posistion, ref PhysicsVelocity velocity) =>
            {
                currentPositionBall[i++] = posistion.Value;
            }).WithoutBurst().Run();
        }

        private void DoSoftIDByPositionXBall()
        {
            float[] array = new float[16];
            int i = 0;
            Entities.ForEach((ref Ball ball, ref Translation posistion) =>
            {
                array[i++] = posistion.Value.x;
            }).WithoutBurst().Run();
            QuickSoftId(array, out softIdByPositionXBallAtElement);
            softIdByPositionXBallAtSerial = new int[array.Length];
            for (i = 0; i < softIdByPositionXBallAtSerial.Length; i++)
            {
                softIdByPositionXBallAtSerial[softIdByPositionXBallAtElement[i]] = i;
            }
        }

        private void DoSoftIDByPositionZBall()
        {
            int i = 0;
            float[] array = new float[16];
            Entities.ForEach((ref Ball ball, ref Translation posistion, ref Rotation rotation, ref NonUniformScale scale) =>
            {
                array[i] = posistion.Value.z;
            }).WithoutBurst().Run();
            QuickSoftId(array, out softIdByPositionZBallAtElement);
            softIdByPositionZBallAtSerial = new int[array.Length];
            for (i = 0; i < softIdByPositionZBallAtSerial.Length; i++)
            {
                softIdByPositionZBallAtSerial[softIdByPositionZBallAtElement[i]] = i;
            }
        }

        private void QuickSoftId(float[] array, out int[] serial)
        {
            serial = new int[array.Length];
            for (int i = 0; i < serial.Length; i++)
            {
                serial[i] = i;
            }
            int startPointer = 0;
            int endPointer = array.Length - 1;
            ContinueQuickSoftId(array, ref serial, startPointer, endPointer);
        }

        private void ContinueQuickSoftId(float[] array, ref int[] serial, int startPointer, int endPointer)
        {
            int currentPointer = startPointer;
            int scaner = startPointer + 1;
            int swapSpace;

            while (scaner <= endPointer)
            {
                if (array[serial[currentPointer]] > array[serial[scaner]])
                {
                    swapSpace = serial[scaner];
                    serial[scaner] = serial[currentPointer + 1];
                    serial[currentPointer + 1] = serial[currentPointer];
                    serial[currentPointer++] = swapSpace;
                }
                scaner++;
            }
            if (currentPointer - 1 > startPointer)
            {
                ContinueQuickSoftId(array, ref serial, startPointer, currentPointer - 1);
            }
            if (currentPointer + 1 < endPointer)
            {
                ContinueQuickSoftId(array, ref serial, currentPointer + 1, endPointer);
            }
        }

        private void MoveAllBall()
        {
            int i = 0;
            Entities.ForEach((ref Ball ball) =>
            {
                ball.addForce.y = 0;
                currentVelocityBall[i++] += ball.addForce;
                ball.addForce = float3.zero;
            }).WithoutBurst().Run();
            for (i = 0; i < isApplyFriction.Length; i++)
            {
                isApplyFriction[i] = true;
                currentMoveAtCollisionBall[i] = currentVelocityBall[i];
            }
            for (int loop = 0; loop < 5; loop++)
            {
                for (i = 0; i < currentPositionBall.Length; i++)
                {
                    HandlePocket(i);
                    UpdateRandomSerial(3);
                    if (!isInTrack[i] && IsBallMoving(i))
                    {
                        if (!CheckCollision(i))
                        {
                            currentPositionBall[i] += currentVelocityBall[i];
                            currentExcludeLine[i] = -1;
                        }
                    }
                }
                //if (IsAnyBallTouching())
                //{
                //    System.Console.WriteLine("Touching");
                //}
                HandleTrack();
            }
            IsAnyBallMoving = false;
            for (i = 0; i < currentPositionBall.Length; i++)
            {
                HandlePocket(i);
                if (!isInTrack[i])
                {
                    if (IsBallMoving(i))
                    {
                        if (!CheckCollision(i))
                        {
                            ApplyFrictionVelocityBall(i);
                            currentPositionBall[i] += currentVelocityBall[i];
                            currentExcludeLine[i] = -1;
                        }
                        if (!isInPocket[i])
                        {
                            IsAnyBallMoving = true;
                        }
                    }
                }
            }
            //if (IsAnyBallTouching())
            //{
            //    System.Console.WriteLine("Touching");
            //}
            HandleTrack();
            i = 0;
            Entities.ForEach((ref Ball ball , ref Translation posistion, ref Rotation rot, ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass) =>
            {
                if (currentRotationBall[i].x < currentVelocityBall[i].z)
                {
                    currentRotationBall[i].x += 0.01f;
                    if (currentRotationBall[i].x > currentVelocityBall[i].z)
                    {
                        currentRotationBall[i].x = currentVelocityBall[i].z;
                    }
                }
                else if (currentRotationBall[i].x > currentVelocityBall[i].z)
                {
                    currentRotationBall[i].x -= 0.01f;
                    if (currentRotationBall[i].x < currentVelocityBall[i].z)
                    {
                        currentRotationBall[i].x = currentVelocityBall[i].z;
                    }
                }
                if (currentRotationBall[i].z < -currentVelocityBall[i].x)
                {
                    currentRotationBall[i].z += 0.01f;
                    if (currentRotationBall[i].z > -currentVelocityBall[i].x)
                    {
                        currentRotationBall[i].z = -currentVelocityBall[i].x;
                    }
                }
                else if (currentRotationBall[i].z > -currentVelocityBall[i].x)
                {
                    currentRotationBall[i].z -= 0.01f;
                    if (currentRotationBall[i].z < -currentVelocityBall[i].x)
                    {
                        currentRotationBall[i].z = -currentVelocityBall[i].x;
                    }
                }
                if (currentRotationBall[i].y < 0)
                {
                    currentRotationBall[i].y += 0.0002f;
                    if (currentRotationBall[i].y > 0)
                    {
                        currentRotationBall[i].y = 0;
                    }
                }
                else if (currentRotationBall[i].y > 0)
                {
                    currentRotationBall[i].y -= 0.0002f;
                    if (currentRotationBall[i].y < 0)
                    {
                        currentRotationBall[i].y = 0;
                    }
                }
                physicsVelocity.SetAngularVelocityWorldSpace(physicsMass, rot, currentRotationBall[i] * 1600);
                posistion.Value = currentPositionBall[i++];
            }).WithoutBurst().Run();
        }

        public bool IsAnyBallRollingInTrack()
        {
            int countFreezeBall = 0;
            foreach (bool isInPocket in isInPocket)
            {
                if (isInPocket)
                {
                    countFreezeBall++;
                }
            }
            foreach (bool isFreeze in isFreezeInTrack)
            {
                if (isFreeze)
                {
                    countFreezeBall--;
                }
            }
            if (countFreezeBall > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsBallInPocket(int serial)
        {
            return isInPocket[serial];
        }

        public void SetPositionCueBall(float3 moveTo)
        {
            bool isCueBall = true;
            Entities.ForEach((ref Ball ball, ref Translation posistion) =>
            {
                if (isCueBall)
                {
                    isCueBall = false;
                    posistion.Value = moveTo;
                    currentPositionBall[0] = moveTo;
                }
            }).WithoutBurst().Run();
        }

        private void ApplyFrictionVelocityBall(int serial)
        {
            if (IsBallMoving(serial))
            {
                float powPower = StaticFuntion.GetSizeVector(currentVelocityBall[serial]);
                float friction;
                if (powPower < 0.00001f)
                {
                    currentVelocityBall[serial].x = 0;
                    currentVelocityBall[serial].z = 0;
                }
                else
                {
                    friction = 0.9949f;
                    currentVelocityBall[serial] -= StaticFuntion.GetResizeVector2(currentVelocityBall[serial], 0.00001f);
                    currentVelocityBall[serial].x *= friction;
                    currentVelocityBall[serial].z *= friction;
                }
            }
        }

        private void HandlePocket(int serial)
        {
            if (!isInTrack[serial])
            {
                if (isInPocket[serial])
                {
                    if (timesFallInPocket[serial]++ > 400)
                    {
                        currentVelocityBall[serial] = float3.zero;
                        currentPositionBall[serial].x = TrackLine[0].x - 0.32f;
                        currentPositionBall[serial].z = TrackLine[0].z;
                        if (!IsCollisionAnyBallsInTRack(serial))
                        {
                            isInTrack[serial] = true;
                            currentPositionStartOfPathTrackBall[serial] = currentPositionBall[serial];
                            currentPathOfTrackBall[serial] = 0;
                            currentVelocityBall[serial] = StaticFuntion.GetResizeVector2(TrackLine[0] - currentPositionBall[serial], 0.01f);
                            currentVelocityBall[serial].y = 0;
                            listSerialBallInTrack.Add(serial);
                            currentRotationBall[serial] = float3.zero;
                        }
                    }
                    else
                    if (timesFallInPocket[serial] > 70)
                    {
                        currentVelocityBall[serial] += StaticFuntion.GetResizeVector2(inPocket[serial].directionGoInsideBoard, 0.00003f);
                    }
                    currentPositionBall[serial].y = -5f;
                }
                else
                {
                    int i;
                    float radiusPocket;
                    Entities.ForEach((ref Pocket pocket, ref Translation posistion, ref NonUniformScale scale) =>
                    {
                        radiusPocket = scale.Value.x * 0.5f;
                        for (i = 0; i < currentPositionBall.Length; i++)
                        {
                            if (!isInPocket[i])
                            {
                                if (StaticFuntion.GetPowSizeVector2(posistion.Value - currentPositionBall[i]) < (radiusPocket - radiusBall) * (radiusPocket - radiusBall))
                                {
                                    isInPocket[i] = true;
                                    currentPositionBall[i].y = -5f;
                                    inPocket[i] = pocket;
                                }
                                else
                                if (StaticFuntion.GetPowSizeVector2(posistion.Value - currentPositionBall[i]) < radiusPocket * radiusPocket)
                                {
                                    currentVelocityBall[i] += StaticFuntion.GetResizeVector2(posistion.Value - currentPositionBall[i], 0.00001f);
                                }
                            }
                        }
                    }).WithoutBurst().Run();
                }
            }
        }

        private void HandleTrack()
        {
            if (listSerialBallInTrack.Count == 0)
                return;
            bool isFreeze = true;
            int lastSerial = listSerialBallInTrack[0];
            foreach (int i in listSerialBallInTrack)
            {
                if (!isFreezeInTrack[i])
                {
                    if (isFreeze && (i != lastSerial && StaticFuntion.IsCollisionBall(currentPositionBall[lastSerial], currentPositionBall[i]) || currentPathOfTrackBall[i] >= TrackLine.Length))
                    {
                        currentVelocityBall[i] = float3.zero;
                        isFreezeInTrack[i] = true;
                        if (i != lastSerial)
                        {
                            currentPositionBall[i] = currentPositionBall[lastSerial] + StaticFuntion.GetResizeVector2(currentPositionBall[i] - currentPositionBall[lastSerial], radiusBall * 2f);
                        }
                    }
                    else
                    {
                        isFreeze = false;
                        currentPositionBall[i] += currentVelocityBall[i];
                        if (StaticFuntion.GetPowSizeVector2(currentPositionBall[i] - currentPositionStartOfPathTrackBall[i]) >= StaticFuntion.GetPowSizeVector2(TrackLine[currentPathOfTrackBall[i]] - currentPositionStartOfPathTrackBall[i]))
                        {
                            currentPathOfTrackBall[i]++;
                        }
                        if (currentPathOfTrackBall[i] < TrackLine.Length)
                        {
                            currentVelocityBall[i] = StaticFuntion.GetResizeVector2(TrackLine[currentPathOfTrackBall[i]] - currentPositionBall[i], 0.01f);
                            currentVelocityBall[i].y = 0;
                            currentPositionStartOfPathTrackBall[i] = currentPositionBall[i];
                        }
                    }
                }
                lastSerial = i;
            }
        }

        public void GetBallOutOfTrack(int serial)
        {
            bool isFound = false;
            for (int i = 0; i < listSerialBallInTrack.Count; i++)
            {
                if (listSerialBallInTrack[i] == serial)
                {
                    isFound = true;
                }
                if(isFound)
                    isFreezeInTrack[listSerialBallInTrack[i]] = false;
            }
            listSerialBallInTrack.Remove(serial);
            isInTrack[serial] = false;
            isInPocket[serial] = false;
            timesFallInPocket[serial] = 0;
        }

        private bool IsCollisionAnyBallsInTRack(int serial)
        {
            for (int i = 0; i < currentPositionBall.Length; i++)
            {
                if (i != serial && isInTrack[i] && StaticFuntion.IsCollisionBall(currentPositionBall[serial],currentPositionBall[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateRandomSerial(int rollTimes)
        {
            int serial1;
            int serial2;
            int space;
            for (int i = 0; i < rollTimes; i++)
            {
                serial1 = random.NextInt(0, ramdomSerial.Length);
                serial2 = random.NextInt(0, ramdomSerial.Length - 1);
                if (serial2 >= serial1)
                {
                    serial2++;
                }
                space = ramdomSerial[serial1];
                ramdomSerial[serial1] = ramdomSerial[serial2];
                ramdomSerial[serial2] = space;
            }
        }

        private bool CheckCollision(int serial, int excludeCheck = -1)
        {

            float3 positionHit;
            bool isHit;
            float powClosest = float.MaxValue;
            float powClosest2 = float.MaxValue;
            float powClosest3 = float.MaxValue;
            float curentPowClowsest;
            float3 velocityA = float3.zero;
            float3 velocityB1 = float3.zero;
            float3 velocityB2 = float3.zero;
            float3 velocityB3 = float3.zero;
            float3 positionHitBall = float3.zero;
            int serialClosest1 = -1;
            int serialClosest2 = -1;
            int serialClosest3 = -1;
            int countCloset = 0;
            bool isHitBall = false;
            bool isHitBall1 = false;
            bool isHitBall2 = false;
            bool isHitBall3 = false;

            if (!isInPocket[serial] && IsBallMoving(serial))
            {
                for (int i = 0; i < currentPositionBall.Length; i++)
                {
                    if (!isInPocket[i] && serial != i && i != excludeCheck)
                    {
                        StaticFuntion.GetHitPositionBallToBall(currentPositionBall[serial], currentPositionBall[i], currentVelocityBall[serial], out isHitBall, out positionHit);
                        if (isHitBall)
                        {
                            curentPowClowsest = StaticFuntion.GetPowSizeVector2(positionHit - currentPositionBall[serial]);
                            if (powClosest > curentPowClowsest)
                            {
                                if (powClosest2 > powClosest)
                                {
                                    if (powClosest3 > powClosest2)
                                    {
                                        powClosest3 = powClosest2;
                                        serialClosest3 = serialClosest2;
                                    }

                                    powClosest2 = powClosest;
                                    serialClosest2 = serialClosest1;
                                }
                                else if (powClosest3 > powClosest)
                                {
                                    powClosest3 = powClosest;
                                    serialClosest3 = serialClosest2;
                                }

                                powClosest = curentPowClowsest;
                                positionHitBall = positionHit;
                                serialClosest1 = i;
                                countCloset = 1;
                            }
                            else
                            {
                                if (powClosest2 > curentPowClowsest)
                                {
                                    if (powClosest3 > powClosest2)
                                    {
                                        powClosest3 = powClosest2;
                                        serialClosest3 = serialClosest2;
                                    }

                                    powClosest2 = curentPowClowsest;
                                    serialClosest2 = i;
                                }
                                else if (powClosest3 > curentPowClowsest)
                                {
                                    powClosest3 = curentPowClowsest;
                                    serialClosest3 = i;
                                }
                            }
                        }
                    }
                }
            }

            if (countCloset > 0)
            {
                if (powClosest <= powClosest2 && powClosest >= powClosest2 - 0.00001f)
                {
                    countCloset++;
                }
                if (powClosest <= powClosest3 && powClosest >= powClosest3 - 0.00001f)
                {
                    countCloset++;
                }
            }

            GetHitPositionBallToBoardLine(serial, -1, out isHit, out positionHit, out int serialBoadLine);
            isHitBall = false;
            if (countCloset > 0)
            {
                if (StaticFuntion.GetPowSizeVector2(positionHitBall - currentPositionBall[serial]) < StaticFuntion.GetPowSizeVector2(currentVelocityBall[serial]))
                {
                    float3 velocityA1 = float3.zero;
                    float3 velocityA2 = float3.zero;
                    float3 velocityA3 = float3.zero;
                    float3 velocityA11 = currentVelocityBall[serial];
                    float3 velocityB11 = currentVelocityBall[serialClosest1];
                    float3 velocityA12 = velocityA11;
                    float3 velocityB12 = velocityB11;
                    StaticFuntion.VelocityAfterCollisionBall(currentPositionBall[serial], currentPositionBall[serialClosest1], currentVelocityBall[serial],
                    out isHitBall1, out float3 positionHitBall1, ref velocityA11, ref velocityB11);
                    if (isHitBall1)
                    {
                        isHitBall = true;
                        StaticFuntion.VelocityAfterCollisionBall(currentPositionBall[serialClosest1], currentPositionBall[serial], currentVelocityBall[serialClosest1],
                        out bool isCurrentHitBall, out positionHitBall1, ref velocityB12, ref velocityA12);
                        if (isCurrentHitBall)
                        {
                            velocityA1 = (velocityA11 + currentVelocityBall[serialClosest1] + velocityA12 + currentVelocityBall[serial]) * 0.5f;
                            velocityB1 = (velocityB11 + currentVelocityBall[serialClosest1] + velocityB12 + currentVelocityBall[serial]) * 0.5f;
                        }
                        else
                        {
                            velocityA1 = velocityA11 + currentVelocityBall[serialClosest1];
                            velocityB1 = velocityB11 + currentVelocityBall[serialClosest1];
                        }
                    }
                    if (countCloset > 1)
                    {
                        float3 velocityA21 = currentVelocityBall[serial];
                        float3 velocityB21 = currentVelocityBall[serialClosest2];
                        float3 velocityA22 = velocityA21;
                        float3 velocityB22 = velocityB21;
                        StaticFuntion.VelocityAfterCollisionBall(currentPositionBall[serial], currentPositionBall[serialClosest2], currentVelocityBall[serial],
                        out isHitBall2, out positionHitBall1, ref velocityA21, ref velocityB21);
                        if (isHitBall2)
                        {
                            isHitBall = true;
                            StaticFuntion.VelocityAfterCollisionBall(currentPositionBall[serialClosest2], currentPositionBall[serial], currentVelocityBall[serialClosest2],
                            out bool isCurrentHitBall, out positionHitBall1, ref velocityB22, ref velocityA22);
                            if (isCurrentHitBall)
                            {
                                velocityA2 = (velocityA21 + currentVelocityBall[serialClosest2] + velocityA22 + currentVelocityBall[serial]) * 0.5f;
                                velocityB2 = (velocityB21 + currentVelocityBall[serialClosest2] + velocityB22 + currentVelocityBall[serial]) * 0.5f;
                            }
                            else
                            {
                                velocityA2 = velocityA21 + currentVelocityBall[serialClosest2];
                                velocityB2 = velocityB21 + currentVelocityBall[serialClosest2];
                            }
                        }
                        if (countCloset > 2)
                        {
                            float3 velocityA31 = currentVelocityBall[serial];
                            float3 velocityB31 = currentVelocityBall[serialClosest3];
                            float3 velocityA32 = velocityA11;
                            float3 velocityB32 = velocityB11;
                            StaticFuntion.VelocityAfterCollisionBall(currentPositionBall[serial], currentPositionBall[serialClosest3], currentVelocityBall[serial],
                            out isHitBall3, out positionHitBall1, ref velocityA31, ref velocityB31);
                            if (isHitBall3)
                            {
                                isHitBall = true;
                                StaticFuntion.VelocityAfterCollisionBall(currentPositionBall[serialClosest3], currentPositionBall[serial], currentVelocityBall[serialClosest3],
                                out bool isCurrentHitBall, out positionHitBall1, ref velocityB32, ref velocityA32);
                                if (isCurrentHitBall)
                                {
                                    velocityA3 = (velocityA31 + currentVelocityBall[serialClosest3] + velocityA32 + currentVelocityBall[serial]) * 0.5f;
                                    velocityB3 = (velocityB31 + currentVelocityBall[serialClosest3] + velocityB32 + currentVelocityBall[serial]) * 0.5f;
                                }
                                else
                                {
                                    velocityA3 = velocityA31 + currentVelocityBall[serialClosest3];
                                    velocityB3 = velocityB31 + currentVelocityBall[serialClosest3];
                                }
                            }
                        }
                    }
                    if (isHitBall)
                    {
                        velocityA = velocityA1 + velocityA2 + velocityA3;
                        float ratioVelocity = (StaticFuntion.GetSizeVector2(currentVelocityBall[serial])
                            + (isHitBall1 ? StaticFuntion.GetSizeVector2(currentVelocityBall[serialClosest1]) : 0)
                            + (isHitBall2 ? StaticFuntion.GetSizeVector2(currentVelocityBall[serialClosest2]) : 0)
                            + (isHitBall3 ? StaticFuntion.GetSizeVector2(currentVelocityBall[serialClosest3]) : 0)) / (StaticFuntion.GetSizeVector2(velocityA)
                            + (isHitBall1 ? StaticFuntion.GetSizeVector2(velocityB1) : 0)
                            + (isHitBall2 ? StaticFuntion.GetSizeVector2(velocityB2) : 0)
                            + (isHitBall3 ? StaticFuntion.GetSizeVector2(velocityB3) : 0));
                        velocityA *= ratioVelocity;
                        velocityB1 *= ratioVelocity;
                        velocityB2 *= ratioVelocity;
                        velocityB3 *= ratioVelocity;
                    }
                }
            }
            if (isHitBall)
            {
                if (isHit && StaticFuntion.GetPowSizeVector2(positionHit - currentPositionBall[serial]) < StaticFuntion.GetPowSizeVector2(positionHitBall - currentPositionBall[serial]))
                {
                    float3 lastVelocity = currentVelocityBall[serial];
                    HandleCollisionBallvsBoardLine(serial, positionHit, serialBoadLine);
                    lastVelocity = StaticFuntion.GetResizeVector2(lastVelocity, StaticFuntion.GetSizeVector2(currentVelocityBall[serial]));
                    float3 checkRoll = currentVelocityBall[serial] + lastVelocity;
                    float maxRoll = StaticFuntion.GetSizeVector2(checkRoll) * 1f;
                    if (math.abs(checkRoll.x) > math.abs(checkRoll.z) && checkRoll.x > 0 || math.abs(checkRoll.x) < math.abs(checkRoll.z) && checkRoll.z < 0)
                    {
                        if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, currentVelocityBall[serial]))
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.3f);
                        }
                        else
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.3f);
                        }
                    }
                    else
                    {
                        if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, currentVelocityBall[serial]))
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.3f);
                        }
                        else
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.3f);
                        }
                    }
                    //if (!CheckCollision(serial))
                    //{
                    //    currentPositionBall[serial] += currentVelocityBall[serial];
                    //}
                }
                else
                {
                    if (serial == 0)
                    {
                        bool isHaveNull = false;
                        foreach (PhysicsEvent physicsEvent in physicsEvents)
                        {
                            if (physicsEvent != null)
                            {
                                if(isHitBall1)
                                    physicsEvent.OnBall0HitAtBall(serialClosest1);
                                if(isHitBall2)
                                    physicsEvent.OnBall0HitAtBall(serialClosest2);
                                if(isHitBall3)
                                    physicsEvent.OnBall0HitAtBall(serialClosest3);
                            }
                            else
                            {
                                isHaveNull = true;
                            }
                        }
                        if (isHaveNull)
                        {
                            physicsEvents.Remove(null);
                        }
                    }
                    isHit = true;
                    float3 lastVelocity = StaticFuntion.GetResizeVector2(currentVelocityBall[serial], StaticFuntion.GetSizeVector2(velocityA));
                    float3 checkRoll = velocityA + lastVelocity;
                    float maxRoll = StaticFuntion.GetSizeVector2(checkRoll) * 1f;
                    if (math.abs(checkRoll.x) > math.abs(checkRoll.z) && checkRoll.x > 0 || math.abs(checkRoll.x) < math.abs(checkRoll.z) && checkRoll.z < 0)
                    {
                        if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, velocityA))
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.1f);
                        }
                        else
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.1f);
                        }
                    }
                    else
                    {
                        if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, velocityA))
                        {
                            if (currentRotationBall[serial].y > StaticFuntion.GetSizeVector2(checkRoll) * 1f)
                                currentRotationBall[serial].y -= StaticFuntion.GetSizeVector2(checkRoll) * 0.1f;
                        }
                        else
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.1f);
                        }
                    }


                    if (isHitBall1)
                    {
                        lastVelocity = StaticFuntion.GetResizeVector2(currentVelocityBall[serial], StaticFuntion.GetSizeVector2(velocityA));
                        checkRoll = velocityB1 + lastVelocity;
                        maxRoll = StaticFuntion.GetSizeVector2(checkRoll) * 1f;
                        if (math.abs(checkRoll.x) > math.abs(checkRoll.z) && checkRoll.x > 0 || math.abs(checkRoll.x) < math.abs(checkRoll.z) && checkRoll.z < 0)
                        {
                            if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, velocityB1))
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.1f);
                            }
                            else
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.1f);
                            }
                        }
                        else
                        {
                            if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, velocityB1))
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.1f);
                            }
                            else
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.1f);
                            }
                        }
                        currentVelocityBall[serialClosest1] = velocityB1;
                        currentExcludeLine[serialClosest1] = -1;
                        isApplyFriction[serialClosest1] = false;
                    }

                    if (isHitBall2)
                    {
                        lastVelocity = StaticFuntion.GetResizeVector2(currentVelocityBall[serial], StaticFuntion.GetSizeVector2(velocityA));
                        checkRoll = velocityB2 + lastVelocity;
                        maxRoll = StaticFuntion.GetSizeVector2(checkRoll) * 1f;
                        if (math.abs(checkRoll.x) > math.abs(checkRoll.z) && checkRoll.x > 0 || math.abs(checkRoll.x) < math.abs(checkRoll.z) && checkRoll.z < 0)
                        {
                            if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, velocityB2))
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.1f);
                            }
                            else
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.1f);
                            }
                        }
                        else
                        {
                            if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, velocityB2))
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.1f);
                            }
                            else
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.1f);
                            }
                        }
                        currentVelocityBall[serialClosest2] = velocityB2;
                        currentExcludeLine[serialClosest2] = -1;
                        isApplyFriction[serialClosest2] = false;
                    }

                    if (isHitBall3)
                    {
                        lastVelocity = StaticFuntion.GetResizeVector2(currentVelocityBall[serial], StaticFuntion.GetSizeVector2(velocityA));
                        checkRoll = velocityB3 + lastVelocity;
                        maxRoll = StaticFuntion.GetSizeVector2(checkRoll) * 1f;
                        if (math.abs(checkRoll.x) > math.abs(checkRoll.z) && checkRoll.x > 0 || math.abs(checkRoll.x) < math.abs(checkRoll.z) && checkRoll.z < 0)
                        {
                            if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, velocityB3))
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.1f);
                            }
                            else
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.1f);
                            }
                        }
                        else
                        {
                            if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, velocityB3))
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.1f);
                            }
                            else
                            {
                                currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.1f);
                            }
                        }
                        currentVelocityBall[serialClosest3] = velocityB3;
                        currentExcludeLine[serialClosest3] = -1;
                        isApplyFriction[serialClosest3] = false;
                    }
                    currentVelocityBall[serial] = velocityA;
                    currentExcludeLine[serial] = -1;
                }
            }
            else
            {
                if (isHit)
                {
                    float3 lastVelocity = currentVelocityBall[serial];
                    HandleCollisionBallvsBoardLine(serial, positionHit, serialBoadLine);
                    lastVelocity = StaticFuntion.GetResizeVector2(lastVelocity, StaticFuntion.GetSizeVector2(currentVelocityBall[serial]));
                    float3 checkRoll = currentVelocityBall[serial] + lastVelocity;
                    float maxRoll = StaticFuntion.GetSizeVector2(checkRoll) * 1f;
                    if (math.abs(checkRoll.x) > math.abs(checkRoll.z) && checkRoll.x > 0 || math.abs(checkRoll.x) < math.abs(checkRoll.z) && checkRoll.z < 0)
                    {
                        if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, currentVelocityBall[serial]))
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.3f);
                        }
                        else
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.3f);
                        }
                    }
                    else
                    {
                        if (StaticFuntion.IsPositionOnTheLeftOfLine2(float3.zero, -lastVelocity, currentVelocityBall[serial]))
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, -maxRoll, 0.3f);
                        }
                        else
                        {
                            currentRotationBall[serial].y = math.lerp(currentRotationBall[serial].y, maxRoll, 0.3f);
                        }
                    }
                    //if (!CheckCollision(serial))
                    //{
                    //    currentPositionBall[serial] += currentVelocityBall[serial];
                    //}
                }
                else
                {
                    currentExcludeLine[serial] = -1;
                }
            }
            if (isHit)
            {
                isApplyFriction[serial] = false;
            }

            return isHit;
        }

        private void HandleCollisionBallvsBoardLine(int serial, float3 positionHit, int serialBoadLine)
        {
            bool isHaveResult;
            positionHit.y = 0;
            float3 positionIn = positionHit - currentVelocityBall[serial];
            float3 directionVectorLine;
            if (serialBoadLine < BoardLine.Length - 1)
            {
                directionVectorLine = BoardLine[serialBoadLine + 1] - BoardLine[serialBoadLine];
            }
            else
            {
                directionVectorLine = BoardLine[0] - BoardLine[BoardLine.Length - 1];
            }
            float a1 = -directionVectorLine.z;
            float b1 = directionVectorLine.x;
            float c1 = -a1 * positionHit.x - b1 * positionHit.z;
            float a2 = directionVectorLine.x;
            float b2 = directionVectorLine.z;
            float c2 = -a2 * positionIn.x - b2 * positionIn.z;
            float3 positionDrop = GetIntersectionPosition(a1, b1, c1, a2, b2, c2, out isHaveResult);
            positionDrop = positionHit + positionHit - positionDrop;
            c1 = -a1 * positionIn.x - b1 * positionIn.z;
            c2 = -a2 * positionDrop.x - b2 * positionDrop.z;
            positionDrop = GetIntersectionPosition(a1, b1, c1, a2, b2, c2, out isHaveResult);
            currentPositionBall[serial] = positionHit - StaticFuntion.GetResizeVector2(currentVelocityBall[serial], 0.001f);
            currentVelocityBall[serial] = positionDrop - positionHit;
            currentExcludeLine[serial] = serialBoadLine;
            if (isInPocket[serial])
            {
                currentVelocityBall[serial] *= 0.2f;
            }
            else
            {
                currentVelocityBall[serial] *= 0.9f;
            }
        }

        private void GetHitPositionBallToBoardLine(int serialBall, int serialExcludeBoadLine, out bool isHit, out float3 positionHit, out int serialBoadLine)
        {
            float3 nextPositionBall = currentPositionBall[serialBall] + currentVelocityBall[serialBall];
            if (nextPositionBall.z > -1.7 && nextPositionBall.z < 2 && nextPositionBall.x > -4.5f && nextPositionBall.x < 4.5f)
            {
                isHit = false;
                positionHit = float3.zero;
                serialBoadLine = -1;
                return;
            }
            serialBoadLine = 0;
            float3 positionBall = currentPositionBall[serialBall];
            float3 closestPositionHit = float3.zero;
            isHit = false;
            bool currentCheckHit;
            bool isFirstHit = true;
            if (serialExcludeBoadLine != BoardLine.Length - 1)
            {
                StaticFuntion.GetHitPositionLineToLine(positionBall, nextPositionBall, BoardLine[BoardLine.Length - 1], BoardLine[0], out currentCheckHit, out positionHit);
                if (currentCheckHit)
                {
                    if (isFirstHit || math.abs(closestPositionHit.x - positionBall.x) > math.abs(positionHit.x - positionBall.x) ||
                        math.abs(closestPositionHit.z - positionBall.z) > math.abs(positionHit.z - positionBall.z))
                    {
                        isFirstHit = false;
                        isHit = true;
                        closestPositionHit = positionHit;
                        serialBoadLine = BoardLine.Length - 1;
                    }
                }
            }

            for (int i = 0; i < BoardLine.Length - 1; i++)
            {
                if (serialExcludeBoadLine != i)
                {
                    StaticFuntion.GetHitPositionLineToLine(positionBall, nextPositionBall, BoardLine[i], BoardLine[i + 1], out currentCheckHit, out positionHit);
                    if (currentCheckHit)
                    {
                        if (isFirstHit || math.abs(closestPositionHit.x - positionBall.x) > math.abs(positionHit.x - positionBall.x) ||
                            math.abs(closestPositionHit.z - positionBall.z) > math.abs(positionHit.z - positionBall.z))
                        {
                            isFirstHit = false;
                            isHit = true;
                            closestPositionHit = positionHit;
                            serialBoadLine = i;
                        }
                    }
                }
            }
            positionHit = closestPositionHit;
        }

        public void GetRaycastHitPositionBallToBoardLine(float3 position, float3 direction, out bool isHit, out float3 positionHit)
        {
            float3 nextPositionBall = position + StaticFuntion.GetResizeVector2(direction, 1000);
            float3 closestPositionHit = float3.zero;
            isHit = false;
            bool currentCheckHit;
            bool isFirstHit = true;
            StaticFuntion.GetHitPositionLineToLine(position, nextPositionBall, BoardLine[BoardLine.Length - 1], BoardLine[0], out currentCheckHit, out positionHit);
            if (currentCheckHit)
            {
                if (isFirstHit || math.abs(closestPositionHit.x - position.x) > math.abs(positionHit.x - position.x) ||
                math.abs(closestPositionHit.z - position.z) > math.abs(positionHit.z - position.z))
                {
                    isFirstHit = false;
                    isHit = true;
                    closestPositionHit = positionHit;
                }
            }
            for (int i = 0; i < BoardLine.Length - 1; i++)
            {
                StaticFuntion.GetHitPositionLineToLine(position, nextPositionBall, BoardLine[i], BoardLine[i + 1], out currentCheckHit, out positionHit);
                if (currentCheckHit)
                {
                    if (isFirstHit || math.abs(closestPositionHit.x - position.x) > math.abs(positionHit.x - position.x) ||
                    math.abs(closestPositionHit.z - position.z) > math.abs(positionHit.z - position.z))
                    {
                        isFirstHit = false;
                        isHit = true;
                        closestPositionHit = positionHit;
                    }
                }
            }
            positionHit = closestPositionHit;
        }

        private bool IsBallMoving(int serial)
        {
            return currentVelocityBall[serial].x != 0 || currentVelocityBall[serial].z != 0;
        }

        private bool IsCollisionBall(float3 positionA, float3 positionB)
        {
            float3 vetor = positionA - positionB;
            return vetor.x * vetor.x + vetor.z * vetor.z <= powDiameterBall;
        }

        private bool IsCollisionToAnyBall(int serial)
        {
            for (int i = 0; i < currentPositionBall.Length; i++)
            {
                if (i != serial && !isInTrack[i] && !isInTrack[serial])
                {
                    if (IsCollisionBall(currentPositionBall[serial], i))
                    {
                        Console.WriteLine("Touching " + serial + " " + i);
                        return true;
                    }
                }
            }
            return false;
        }

        public static void QuadraticEquation2(float a, float b, float c, out float x1, out float x2)
        {
            float del = b * b - 4 * a * c;
            if (del >= 0)
            {
                del = math.sqrt(del);
                x1 = (-b + del) / (2 * a);
                x2 = (-b - del) / (2 * a);
            }
            else
            {
                x1 = x2 = float.NaN;
            }

        }

        private float3 GetIntersectionPosition(float a1, float b1, float c1, float a2, float b2, float c2, out bool isHaveResult)
        {
            float3 result = float3.zero;
            isHaveResult = false;
            if (b1 == 0 && b2 == 0)
            {
                return result;
            }
            if (math.abs(b1) > math.abs(b2))
            {
                result.x = a2 * b1 - a1 * b2;
                if (result.x != 0)
                {
                    isHaveResult = true;
                    result.x = (b2 * c1 - b1 * c2) / (a2 * b1 - a1 * b2);
                    result.z = (-a1 * result.x - c1) / b1;
                }
            }
            else
            {
                result.x = a1 * b2 - a2 * b1;
                if (result.x != 0)
                {
                    isHaveResult = true;
                    result.x = (b1 * c2 - b2 * c1) / (a1 * b2 - a2 * b1);
                    result.z = (-a2 * result.x - c2) / b2;
                }
            }
            return result;
        }

        private void ResizeVector2(ref float3 vector, float size)
        {
            float raito = size / math.sqrt(vector.x * vector.x + vector.z * vector.z);
            vector.x *= raito;
            vector.z *= raito;
        }

        private float GetSizeVector(float3 vector)
        {
            return math.sqrt(vector.x * vector.x + vector.z * vector.z + vector.z * vector.z);
        }

        private float GetPowSizeVector2(float3 vector)
        {
            return vector.x * vector.x + vector.z * vector.z;
        }

        private bool IsPositionInsideBoard(float3 position, int serialBoadLine)
        {
            float a1 = BoardLineNormalInside[serialBoadLine].x;
            float b1 = BoardLineNormalInside[serialBoadLine].z;
            float c1 = -a1 * BoardLine[serialBoadLine].x - b1 * BoardLine[serialBoadLine].z;
            float a2 = -BoardLineNormalInside[serialBoadLine].z;
            float b2 = BoardLineNormalInside[serialBoadLine].x;
            float c2 = -a2 * position.x - b2 * position.z;
            float3 vector = position - GetIntersectionPosition(a1, b1, c1, a2, b2, c2, out bool isHaveResult);
            return isHaveResult && (vector.x > 0 && BoardLineNormalInside[serialBoadLine].x > 0 || vector.x < 0 && BoardLineNormalInside[serialBoadLine].x < 0 || vector.z > 0 && BoardLineNormalInside[serialBoadLine].z > 0 || vector.z < 0 && BoardLineNormalInside[serialBoadLine].z < 0);
        }

        private void GetHitPositionLineToLine(float3 positionLineA1, float3 positionLineA2, float3 positionLineB1, float3 positionLineB2, out bool isHit, out float3 positionHit)
        {
            float maxA;
            float minA;
            float maxB;
            float minB;
            float3 vetor = positionLineA1 - positionLineA2;
            float a1 = -vetor.z;
            float b1 = vetor.x;
            float c1 = -a1 * positionLineA1.x - b1 * positionLineA1.z;
            vetor = positionLineB1 - positionLineB2;
            float a2 = -vetor.z;
            float b2 = vetor.x;
            float c2 = -a2 * positionLineB1.x - b2 * positionLineB1.z;
            positionHit = GetIntersectionPosition(a1, b1, c1, a2, b2, c2, out bool isHaveResult);
            if (isHaveResult)
            {
                isHit = true;
                if (positionLineA1.x < positionLineA2.x)
                {
                    maxA = positionLineA2.x;
                    minA = positionLineA1.x;

                    if (positionHit.x > maxA || positionHit.x < minA)
                    {
                        isHit = false;
                    }
                }
                else if (positionLineA1.x > positionLineA2.x)
                {
                    maxA = positionLineA1.x;
                    minA = positionLineA2.x;

                    if (positionHit.x > maxA || positionHit.x < minA)
                    {
                        isHit = false;
                    }
                }
                else
                {
                    if (positionLineA1.z < positionLineA2.z)
                    {
                        maxA = positionLineA2.z;
                        minA = positionLineA1.z;

                        if (positionHit.z > maxA || positionHit.z < minA)
                        {
                            isHit = false;
                        }
                    }
                    else if (positionLineA1.z > positionLineA2.z)
                    {
                        maxA = positionLineA1.z;
                        minA = positionLineA2.z;

                        if (positionHit.z > maxA || positionHit.z < minA)
                        {
                            isHit = false;
                        }
                    }
                }
                if (isHit)
                {
                    if (positionLineB1.x < positionLineB2.x)
                    {
                        maxB = positionLineB2.x;
                        minB = positionLineB1.x;

                        if (positionHit.x > maxB || positionHit.x < minB)
                        {
                            isHit = false;
                        }
                    }
                    else if (positionLineB1.x > positionLineB2.x)
                    {
                        maxB = positionLineB1.x;
                        minB = positionLineB2.x;

                        if (positionHit.x > maxB || positionHit.x < minB)
                        {
                            isHit = false;
                        }
                    }
                    else
                    {
                        if (positionLineB1.z < positionLineB2.z)
                        {
                            maxB = positionLineB2.z;
                            minB = positionLineB1.z;

                            if (positionHit.z > maxB || positionHit.z < minB)
                            {
                                isHit = false;
                            }
                        }
                        else if (positionLineB1.z > positionLineB2.z)
                        {
                            maxB = positionLineB1.z;
                            minB = positionLineB2.z;

                            if (positionHit.z > maxB || positionHit.z < minB)
                            {
                                isHit = false;
                            }
                        }
                    }
                }
            }
            else
            {
                isHit = false;
            }
        }

        public bool IsAnyBallTouching()
        {
            for (int i = 0; i < currentPositionBall.Length; i++)
            {
                if (IsCollisionToAnyBall(i))
                {
                    return true;
                }
            }
            return false;
        }

        public float3 GetPositionBall(int serial)
        {
            if (serial < currentPositionBall.Length)
            {
                return currentPositionBall[serial];
            }
            else
            {
                return new float3(float.NaN,float.NaN,float.NaN);
            }
        }

        public void AddEventListener(PhysicsEvent physicsEvent)
        {
            if(!physicsEvents.Contains(physicsEvent))
                physicsEvents.Add(physicsEvent);
        }
    }

    public interface PhysicsEvent
    {
        void OnBall0HitAtBall(int serial);
    }
}