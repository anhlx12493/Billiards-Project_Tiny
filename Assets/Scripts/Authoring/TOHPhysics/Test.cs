using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public void Start()
    {
        var test1 = GetComponent<Test1>();
        Debug.Log(test1.test);
    }
}
