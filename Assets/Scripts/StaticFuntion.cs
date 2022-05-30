
using System;
using Unity.Mathematics;

namespace Billiards
{
    public static class StaticFuntion
    {
        public readonly static float radiusBall = 0.16f;
        public readonly static float diameterBall = 0.32f;
        public readonly static float radiusPocket = 0.35f;
        public readonly static float powRadiusPocket = 0.1125f;
        public readonly static float powDiameterBall = 0.1024f;
        public readonly static float3 vector_up = new float3(0, 0, 1);



        public static void QuadraticEquation2(float a, float b, float c, out float x1, out float x2, out bool isNaN)
        {
            float del = b * b - 4 * a * c;
            isNaN = false;
            if (a == 0)
            {
                if (b != 0)
                {
                    x1 = x2 = -c / b;
                }
                else
                {
                    x1 = x2 = 0;
                    isNaN = true;
                }
            }
            else
            {
                if (del >= 0)
                {
                    del = (float)math.sqrt(del);
                    x1 = (-b + del) / (2 * a);
                    x2 = (-b - del) / (2 * a);
                }
                else
                {
                    x1 = x2 = 0;
                    isNaN = true;
                }
            }

        }

        public static void GetHitPositionBallToBall(float3 posStart, float3 posTarget, float3 force, out bool isHit, out float3 result)
        {
            float a = force.z;
            float b = -force.x;
            float c = -a * posStart.x - b * posStart.z;
            float i1 = posTarget.x;
            float i2 = posTarget.z;
            isHit = false;
            bool isNaN;
            if (math.abs(b) > math.abs(a))
            {
                float p1 = a / b;
                float p2 = c / b;
                float A = 1 + p1 * p1;
                float B = 2 * (a * c / (b * b) - i1 + i2 * a / b);
                float C = p2 * p2 + 2 * i2 * c / b + i1 * i1 + i2 * i2 - powDiameterBall;
                QuadraticEquation2(A, B, C, out float x1, out float x2, out isNaN);
                if (isNaN)
                {
                    result = float3.zero;
                }
                else
                {
                    if (math.abs(x1 - posStart.x) > math.abs(x2 - posStart.x))
                    {
                        float sf = x1;
                        x1 = x2;
                        x2 = sf;
                    }
                    if (posStart.x >= x1 && posStart.x <= x2 || posStart.x <= x1 && posStart.x >= x2)
                    {
                        result.x = posStart.x;
                        result.y = 0;
                        result.z = posStart.z;
                        if ((x2 - posStart.x) * force.x > 0)
                        {
                            isHit = true;
                        }
                    }
                    else
                    {
                        float y = (-a * x1 - c) / b;
                        result = new float3(x1, 0, y);
                        float3 nextPos = posStart + force;
                        if (posStart.x >= result.x && posStart.x >= nextPos.x || posStart.x <= result.x && posStart.x <= nextPos.x)
                        {
                            if (posStart.z >= result.z && posStart.z >= nextPos.z || posStart.z <= result.z && posStart.z <= nextPos.z)
                            {
                                isHit = true;
                            }
                        }
                        if (!isHit)
                        {
                            result = float3.zero;
                        }
                    }
                }
            }
            else
            {
                float p1 = b / a;
                float p2 = c / a;
                float A = 1 + p1 * p1;
                float B = 2 * (b * c / (a * a) - i2 + i1 * b / a);
                float C = p2 * p2 + 2 * i1 * c / a + i2 * i2 + i1 * i1 - powDiameterBall;
                QuadraticEquation2(A, B, C, out float z1, out float z2, out isNaN);
                if (isNaN)
                {
                    result = float3.zero;
                }
                else
                {
                    if (math.abs(z1 - posStart.z) > math.abs(z2 - posStart.z))
                    {
                        float sf = z1;
                        z1 = z2;
                        z2 = sf;
                    }
                    if (posStart.z >= z1 && posStart.z <= z2 || posStart.z <= z1 && posStart.z >= z2)
                    {
                        result.x = posStart.x;
                        result.y = 0;
                        result.z = posStart.z;
                        if ((z2 - posStart.z) * force.z > 0)
                        {
                            isHit = true;
                        }
                    }
                    else
                    {
                        float x = (-b * z1 - c) / a;
                        result = new float3(x, 0, z1);
                        float3 nextPos = posStart + force;
                        if (posStart.x >= result.x && posStart.x >= nextPos.x || posStart.x <= result.x && posStart.x <= nextPos.x)
                        {
                            if (posStart.z >= result.z && posStart.z >= nextPos.z || posStart.z <= result.z && posStart.z <= nextPos.z)
                            {
                                isHit = true;
                            }
                        }
                        if (!isHit)
                        {
                            result = float3.zero;
                        }
                    }
                }
            }
        }

        public static bool IsCollisionBall(float3 positionA, float3 positionB)
        {
            float3 vetor = positionA - positionB;
            return vetor.x * vetor.x + vetor.z * vetor.z <= powDiameterBall;
        }

        public static bool IsCollisionBall(float3 positionA, float3 positionB, float extraDistance)
        {
            float3 vetor = positionA - positionB;
            return vetor.x * vetor.x + vetor.z * vetor.z <= math.pow((radiusBall + extraDistance) * 2, 2);
        }

        public static bool IsInPocket(float3 position, float3 positionPocket, float radiusPocket)
        {
            float3 vetor = position - positionPocket;
            return vetor.x * vetor.x + vetor.z * vetor.z <= radiusPocket * radiusPocket;
        }

        public static void VelocityAfterCollisionBall(float3 positionRoot, float3 positionTarget, float3 velocityRootIn,
            out bool isHit, out float3 positionHit, ref float3 velocityRootOut, ref float3 velocityTargetOut)
        {
            positionRoot -= velocityRootIn;
            GetHitPositionBallToBall(positionRoot, positionTarget, velocityRootIn, out isHit, out positionHit);
            if (isHit)
            {
                float3 vetor = positionTarget - positionHit;
                float3 R = positionHit - velocityRootIn;
                float a1 = -vetor.z;
                float b1 = vetor.x;
                float c1 = -a1 * R.x - b1 * R.z;
                float a2 = vetor.x;
                float b2 = vetor.z;
                float c2 = -a2 * positionHit.x - b2 * positionHit.z;
                float x, y;
                if (math.abs(b1) > math.abs(a1))
                {
                    x = (b2 * c1 - b1 * c2) / (a2 * b1 - a1 * b2);
                    y = (-a1 * x - c1) / b1;
                }
                else
                {
                    y = (a2 * c1 - a1 * c2) / (a1 * b2 - a2 * b1);
                    x = (-b1 * y - c1) / a1;
                }
                float3 O = new float3(x, 0, y);
                velocityRootOut = positionHit - O;
                velocityTargetOut = O - R;
                //if (x != 0 || y != 0)
                //{
                //    a1 = GetSizeVector2(velocityRootIn) / (GetSizeVector2(velocityRootOut) + GetSizeVector2(velocityTargetOut));
                //}
                //else
                //{
                //    a1 = 0;
                //}
                //velocityRootOut *= a1;
                //velocityTargetOut *= a1;
            }
        }

        public static float3 GetIntersectionPosition(float a1, float b1, float c1, float a2, float b2, float c2, out bool isHaveResult)
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

        public static void ResizeVector2(ref float3 vector, float size)
        {
            float raito = size / math.sqrt(vector.x * vector.x + vector.z * vector.z);
            vector.x *= raito;
            vector.z *= raito;
        }

        public static void RotateVectorWithoutSize2(ref float3 vector, float angle)
        {
            float currentAngle;
            if (math.abs(vector.x) < math.abs(vector.z))
            {
                currentAngle = math.atan(vector.x / vector.z);
                if (currentAngle > 0)
                {
                    if (vector.z < 0)
                    {
                        currentAngle = -math.PI * 0.5f - currentAngle;
                    }
                    else
                    {
                        currentAngle = 2.5f * math.PI - currentAngle;
                    }
                }
                else
                {
                    if (vector.z < 0)
                    {
                        currentAngle =  math.PI * 1.5f - currentAngle;
                    }
                    else
                    {
                        currentAngle = 2.5f * math.PI - currentAngle;
                    }
                }
            }
            else
            {
                currentAngle = math.atan(vector.z / vector.x);
                if (currentAngle > 0)
                {
                    if (vector.x < 0)
                    {
                        currentAngle += math.PI;
                    }
                }
                else
                {
                    if (vector.x < 0)
                    {
                        currentAngle += math.PI;
                    }
                }
            }
            currentAngle += angle;
            while (currentAngle >= math.PI * 2f)
            {
                currentAngle -= math.PI * 2f;
            }
            while (currentAngle < 0)
            {
                currentAngle += math.PI * 2f;
            }
            vector.x = math.cos(currentAngle);
            vector.z = math.sin(currentAngle);
        }

        public static float GetAngle(float3 vectorA,float3 vectorB)
        {
            float currentAngleA;
            if (math.abs(vectorA.x) < math.abs(vectorA.z))
            {
                currentAngleA = math.atan(vectorA.x / vectorA.z);
                if (currentAngleA > 0)
                {
                    if (vectorA.z < 0)
                    {
                        currentAngleA = -math.PI * 0.5f - currentAngleA;
                    }
                    else
                    {
                        currentAngleA = 2.5f * math.PI - currentAngleA;
                    }
                }
                else
                {
                    if (vectorA.z < 0)
                    {
                        currentAngleA = math.PI * 1.5f - currentAngleA;
                    }
                    else
                    {
                        currentAngleA = 2.5f * math.PI - currentAngleA;
                    }
                }
            }
            else
            {
                currentAngleA = math.atan(vectorA.z / vectorA.x);
                if (currentAngleA > 0)
                {
                    if (vectorA.x < 0)
                    {
                        currentAngleA += math.PI;
                    }
                }
                else
                {
                    if (vectorA.x < 0)
                    {
                        currentAngleA += math.PI;
                    }
                }
            }
            while (currentAngleA >= math.PI * 2f)
            {
                currentAngleA -= math.PI * 2f;
            }
            while (currentAngleA < 0)
            {
                currentAngleA += math.PI * 2f;
            }
            float currentAngleB;
            if (math.abs(vectorB.x) < math.abs(vectorB.z))
            {
                currentAngleB = math.atan(vectorB.x / vectorB.z);
                if (currentAngleB > 0)
                {
                    if (vectorB.z < 0)
                    {
                        currentAngleB = -math.PI * 0.5f - currentAngleB;
                    }
                    else
                    {
                        currentAngleB = 2.5f * math.PI - currentAngleB;
                    }
                }
                else
                {
                    if (vectorB.z < 0)
                    {
                        currentAngleB = math.PI * 1.5f - currentAngleB;
                    }
                    else
                    {
                        currentAngleB = 2.5f * math.PI - currentAngleB;
                    }
                }
            }
            else
            {
                currentAngleB = math.atan(vectorB.z / vectorB.x);
                if (currentAngleB > 0)
                {
                    if (vectorB.x < 0)
                    {
                        currentAngleB += math.PI;
                    }
                }
                else
                {
                    if (vectorB.x < 0)
                    {
                        currentAngleB += math.PI;
                    }
                }
            }
            while (currentAngleB >= math.PI * 2f)
            {
                currentAngleB -= math.PI * 2f;
            }
            while (currentAngleB < 0)
            {
                currentAngleB += math.PI * 2f;
            }
            currentAngleB -= currentAngleA;
            if (currentAngleB > math.PI)
            {
                currentAngleB = currentAngleB - math.PI * 2;
            }
            else if (currentAngleB < -math.PI)
            {
                currentAngleB = currentAngleB + math.PI * 2;
            }
            return currentAngleB;
        }

        public static float3 GetResizeVector2(float3 vector, float size)
        {
            if (vector.x == 0 && vector.z == 0)
                return float3.zero;
            float raito = size / math.sqrt(vector.x * vector.x + vector.z * vector.z);
            vector.x *= raito;
            vector.z *= raito;
            return vector;
        }

        public static float GetSizeVector(float3 vector)
        {
            return math.sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
        }

        public static float GetSizeVector2(float3 vector)
        {
            return math.sqrt(vector.x * vector.x + vector.z * vector.z);
        }

        public static float GetPowSizeVector2(float3 vector)
        {
            return vector.x * vector.x + vector.z * vector.z;
        }

        public static void GetHitPositionLineToLine(float3 positionLineA1, float3 positionLineA2, float3 positionLineB1, float3 positionLineB2, out bool isHit, out float3 positionHit)
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

        public static bool IsPositionOnTheLeftOfLine2(float3 position, float3 positionLine1, float3 positionLine2)
        {
            float3 vector = positionLine2 - positionLine1;
            float a = vector.z;
            float b = -vector.x;
            float c = -a * positionLine1.x - b * positionLine1.z;
            if (math.abs(a) > math.abs(b))
            {
                float x = -(b * position.z + c) / a;
                return position.x < x;
            }
            else
            {
                float z = -(a * position.x + c) / b;
                return position.z < z;
            }
        }
    }
}
