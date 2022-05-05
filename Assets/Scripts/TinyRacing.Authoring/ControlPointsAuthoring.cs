using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System;

namespace TinyRacing.Authoring
{
    [DisallowMultipleComponent]
    public class ControlPointsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            
        }

        // Draw the path between control points
        void OnDrawGizmos()
        {
            Console.WriteLine("Hello");
        }
    }
}
