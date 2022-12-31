using System;
using System.Collections.Generic;
using UnityEngine;

public class CompositeBezierPath
{
	const float DEFAULT_CONTROLPOINT_LENGHT = 0.3f; // 0.3 times of distance between points of polyline

	List<CubicBezier> beziers;

	public CompositeBezierPath(List<Vector3> polyline)
	{
		beziers = new List<CubicBezier>();
		if (polyline.Count < 3)
		{
			Debug.LogError("Cannot construct CompositeBezierPath");
			return;
		}
		Vector3 t0 = Vector3.Normalize(polyline[1] - polyline[0]);

		// 0, 1, 2, ..., n-2
		for(int i=0; i<=polyline.Count-2; ++i)
		{
			Vector3 t1;
			if (i != polyline.Count - 2)
			{
                t1 = averageTangent(polyline[i], polyline[i + 1], polyline[i + 2]);
            }
			else
			{
                t1 = Vector3.Normalize(polyline[i + 1] - polyline[i]);
            }
            float d = Vector3.Magnitude(polyline[i + 1] - polyline[i]);
			Vector3 p0 = polyline[i];
            Vector3 p1 = p0 + t0 * d * DEFAULT_CONTROLPOINT_LENGHT;
            Vector3 p3 = polyline[i + 1];
            Vector3 p2 = p3 - t1 * d * DEFAULT_CONTROLPOINT_LENGHT;

            CubicBezier cb = new CubicBezier(p0, p1, p2, p3);
            beziers.Add(cb);
            t0 = t1;
        }
    }

	public List<Vector3> Rasterize(float resolution = 0.2f)
	{
		List<Vector3> output = new List<Vector3>();
		foreach (var b in beziers)
		{
			for(float t=0; t<1; t += resolution)
			{
				output.Add(b.Interpolate(t));
            }
		}
		output.Add(beziers[beziers.Count - 1].controlPoints[3]);
		return output;
	}

	Vector3 averageTangent(Vector3 p0, Vector3 p1, Vector3 p2)
	{
		Vector3 t1 = Vector3.Normalize(p1 - p0);
        Vector3 t2 = Vector3.Normalize(p2 - p1);
		return (t1 + t2) / 2;
	}
}

// Reference : https://en.wikipedia.org/wiki/B%C3%A9zier_curve
public class CubicBezier
{
	public readonly List<Vector3> controlPoints;

	public CubicBezier (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		controlPoints = new List<Vector3>() { p0,p1,p2,p3 };
	}

	public Vector3 Interpolate(float t)
	{
		return Mathf.Pow((1 - t), 3) * controlPoints[0] + 3 * (1 - t) * (1 - t) * t * controlPoints[1] + 3 * (1 - t) * t * t * controlPoints[2] + t * t * t * controlPoints[3];
	}

	public Vector3 Velocity(float t)
	{
		return 3 * (1 - t) * (1 - t) * (controlPoints[1] - controlPoints[0]) + 6 * (1 - t) * t * (controlPoints[2] - controlPoints[1]) + 3 * t * t * (controlPoints[3] - controlPoints[2]);
	}

	public Vector3 Acceleration(float t)
	{
		return 6 * (1 - t) * (controlPoints[2] - 2 * controlPoints[1] + controlPoints[0]) + 6 * t * (controlPoints[3] - 2 * controlPoints[2] + controlPoints[1]);
	}
}