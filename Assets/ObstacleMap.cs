using System;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleMap
{
	Bounds bound;
	float gridSize = 0.1f;
	List<GameObject> obstracles;
	bool[,] map;

	public ObstacleMap(Bounds _bound, List<GameObject> _obstracles, float _gridSize)
	{
		bound = _bound;
		gridSize = _gridSize;
		obstracles = _obstracles;

		int w = (int)(bound.size.x / gridSize);
        int h = (int)(bound.size.z / gridSize);

        // init map
        map = new bool[h,w];
        for (int j=0; j<h; ++j )
		{
			for (int i=0; i<w; ++i)
			{
				if (map[j,i])
					continue;

				foreach (var obstracle in obstracles)
				{
					map[j,i] = IsCollide(obstracle, indexToPosition(i, j));
					if (map[j,i])
						break;
				}
			}
		}
	}

	public void PrintMap()
	{
		string output = "";
		for(int j=map.GetLength(0)-1; j>=0; j--)
		{
			for (int i=0; i < map.GetLength(1); ++i)
			{
				output += map[j, i] ? "X " : "- ";
			}
			output += "\n";
		}
		Debug.Log(output);
	}

	Vector3 indexToPosition(int i, int j)
	{
		return new Vector3(i, 0, j) * gridSize + bound.min;
	}

	public bool IsCollide(Vector3 pos)
	{
		pos -= bound.min;
		pos /= gridSize;
		return map[(int)pos.z, (int)pos.x];
	}

	bool IsCollide(GameObject go, Vector3 point)
	{
        var b = go.GetComponent<Renderer>().bounds;
		return b.Contains(point);
    }

}

