using System;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleMap
{
	Bounds bound;
	float gridSize = 0.1f;
	float safeZoneSize = 0.5f;
	List<GameObject> obstracles;
	bool[,] map;			// true if there's obstacle

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

		//applySafeZone();
	}

	public bool IsCollide(Vector3 pos)
	{
		pos -= bound.min;
		pos /= gridSize;
		return map[(int)pos.z, (int)pos.x];
	}

	public bool IsPathCollide(Vector3 start, Vector3 end)
	{
		Vector3 step = Vector3.Normalize(end - start) * gridSize;
		float u_abs = Vector3.Magnitude(end - start);

		float distance = 0;
		Vector3 x = start;
		while (distance < u_abs)
		{
			distance += gridSize;
			x += step;
			if (IsCollide(x))
				return true;
		}
		return false;
	}

	static public void PrintMap(bool[,] aMap)
	{
		string output = "";
		for(int j= aMap.GetLength(0)-1; j>=0; j--)
		{
			for (int i=0; i < aMap.GetLength(1); ++i)
			{
				output += aMap[j, i] ? "X " : "- ";
			}
			output += "\n";
		}
		Debug.Log(output);
	}

	Vector3 indexToPosition(int i, int j)
	{
		return new Vector3(i, 0, j) * gridSize + bound.min;
	}

	bool IsCollide(GameObject go, Vector3 point)
	{
        var b = go.GetComponent<Renderer>().bounds;
		return b.Contains(point);
    }

	void applySafeZone()
	{
		bool[,] newMap = new bool[map.GetLength(0), map.GetLength(1)];
		int safeGridCount = (int) (safeZoneSize / gridSize);
		Debug.Log(safeGridCount);
		for (int j=0; j<map.GetLength(0); ++j)
		{
			for (int i=0; i<map.GetLength(1); ++i)
			{
				if (map[i,j])
				{
					for (int dy=-safeGridCount; dy<=safeGridCount; ++dy)
					{
						for (int dx=-safeGridCount; dx<=safeGridCount; ++dx)
						{
							if (newMap[i, j])
								continue;

							int newX = i + dx;
							int newY = j + dy;

							if (newX >= 0 && newX < map.GetLength(1) && newY >= 0 && newY < map.GetLength(0) )
								newMap[newX, j=newY] = true;
						}
					}
				}
			}
		}
        PrintMap(map);
        PrintMap(newMap);
        map = newMap;

	}

}

