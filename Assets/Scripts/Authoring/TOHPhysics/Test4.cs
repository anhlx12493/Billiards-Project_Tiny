using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Test4 : SystemBase
{

    protected override void OnUpdate()
    {
        Entities.ForEach((ref Test3 test) =>
        {
            test.test.Start();
        }).WithoutBurst().Run();
        Debug.Log("AAAAAAAAAAAAAA");
    }
}
