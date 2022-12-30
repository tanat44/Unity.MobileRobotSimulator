using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils {

    public static void Print(List<Vector3> list)
    {
        string output = $"Count: {list.Count}\n";
        foreach (var v in list)
        {
            output += $"> {v} \n";
        }
        Debug.Log(output);
    }
}
