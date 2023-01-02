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

    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
        Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        float s = Vector3.Dot(crossVec3and2, crossVec1and2)
                / crossVec1and2.sqrMagnitude;
        intersection = linePoint1 + (lineVec1 * s);
        return true;

    }

}
