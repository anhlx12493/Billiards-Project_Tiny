﻿using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Tiny;
using Unity.Tiny.Input;
using Unity.Transforms;

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
                    friction = 0.994f;
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
            bool isHit = false;

            if (!isInPocket[serial])
            {
                for (int i = 0; i < currentPositionBall.Length; i++)
                {
                    if (!isInPocket[ramdomSerial[i]] && serial != ramdomSerial[i] && ramdomSerial[i] != excludeCheck)
                    {
                        if (IsCollisionBall(currentPositionBall[serial] + currentMoveAtCollisionBall[serial], currentPositionBall[ramdomSerial[i]] + currentMoveAtCollisionBall[ramdomSerial[i]]))
                        {
                            StaticFuntion.VelocityAfterCollisionBall(currentPositionBall[serial], currentPositionBall[ramdomSerial[i]], currentVelocityBall[serial], currentVelocityBall[ramdomSerial[i]],
                                out bool isHitBall, out positionHit, ref currentVelocityBall[serial], ref currentVelocityBall[ramdomSerial[i]]);
                            if (isHitBall)
                            {
                                isHit = true;
                                CheckCollisionBallvsBoardLine(serial);
                                CheckCollisionBallvsBoardLine(i);
                                currentMoveAtCollisionBall[serial] = 0;
                                currentMoveAtCollisionBall[ramdomSerial[i]] = currentVelocityBall[ramdomSerial[i]];
                                currentPositionBall[serial] = positionHit;
                                isApplyFriction[ramdomSerial[i]] = false;
                                currentExcludeLine[serial] = -1;
                                currentExcludeLine[ramdomSerial[i]] = -1;
                                CheckCollision(serial, ramdomSerial[i]);
                                CheckCollision(ramdomSerial[i], serial);
                            }
                        }
                    }
                }
            }
            if (!isHit && CheckCollisionBallvsBoardLine(serial))
                isHit = true;
            if (isHit)
                isApplyFriction[serial] = false;

            return isHit;
        }

        private bool CheckCollisionBallvsBoardLine(int serial)
        {
            float3 positionHit;
            GetHitPositionBallToBoardLine(serial, currentExcludeLine[serial], out bool isHit, out positionHit, out int serialBoadLine);

            if (isHit)
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
                currentVelocityBall[serial] = positionDrop - positionHit;
                currentPositionBall[serial] = positionHit;
                currentExcludeLine[serial] = serialBoadLine;
                if (isInPocket[serial])
                {
                    currentVelocityBall[serial] *= 0.1f;
                }
            }
            else
            {
                currentExcludeLine[serial] = -1;
            }
            return isHit;
        }

        private void GetHitPositionBallToBoardLine(int serialBall, int serialExcludeBoadLine, out bool isHit, out float3 positionHit, out int serialBoadLine)
        {
            if (currentPositionBall[serialBall].z > -1.7 && currentPositionBall[serialBall].z < 2 && currentPositionBall[serialBall].x > -4.5f && currentPositionBall[serialBall].x < 4.5f)
            {
                isHit = false;
                positionHit = float3.zero;
                serialBoadLine = -1;
                return;
            }
            serialBoadLine = 0;
            float3 nextPositionBall = currentPositionBall[serialBall] + currentVelocityBall[serialBall];
            float3 positionBall = currentPositionBall[serialBall];
            float3 closestPositionHit = float3.zero;
            isHit = false;
            bool currentCheckHit;
            bool isFirstHit = true;
            if (serialExcludeBoadLine != BoardLine.Length - 1)
            {
                GetHitPositionLineToLine(positionBall, nextPositionBall, BoardLine[BoardLine.Length - 1], BoardLine[0], out currentCheckHit, out positionHit);
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
                    GetHitPositionLineToLine(positionBall, nextPositionBall, BoardLine[i], BoardLine[i + 1], out currentCheckHit, out positionHit);
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
            GetHitPositionLineToLine(position, nextPositionBall, BoardLine[BoardLine.Length - 1], BoardLine[0], out currentCheckHit, out positionHit);
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
                GetHitPositionLineToLine(position, nextPositionBall, BoardLine[i], BoardLine[i + 1], out currentCheckHit, out positionHit);
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
                if (i != serial)
                {
                    if (IsCollisionBall(currentPositionBall[serial], i))
                    {
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
    }
}