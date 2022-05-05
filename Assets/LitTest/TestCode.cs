using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Entities;

public class TestCode
{
    //public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    //{
    //    Console.WriteLine("Init successfully");
    //}

    // Start is called before the first frame update
    public void Start()
    {
        Console.WriteLine("Hello, I'm Hung");
        //Vector2 v2 = new Vector2(1, 2);
        //Console.WriteLine(v2.y);

    }

    // Update is called once per frame
    void Update()
    {

    }

}

//public class MeshComponent : IComponentData
//{
//    public Mesh Mesh;
//}

//[ConverterVersion("unity", 1)]
//public class MeshReference : MonoBehaviour, IConvertGameObjectToEntity
//{
//    public Mesh Mesh;

//    public void Convert(Entity entity, EntityManager dstManager,
//        GameObjectConversionSystem conversionSystem)
//    {
//        dstManager.AddComponentData(entity, new MeshComponent
//        {
//            Mesh = Mesh
//        });
//        // No need to declare a dependency here, we're merely referencing an asset.
//    }
//}

