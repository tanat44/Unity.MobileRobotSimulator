using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class PathFinder : MonoBehaviour
{

    Vector3 x0, x1;
    Bounds simulationBound;
    List<Vector3> pathToRender = new List<Vector3>();

    const float GRID_SIZE = 0.1f;

    void Start()
    {
        // Find all obstracles
        var obstracleParent = GameObject.Find("Obstracle");
        List<GameObject> obstracleGo = new List<GameObject>();
        for(int i=0; i<obstracleParent.transform.childCount; ++i)
        {
obstracleGo.Add(obstracleParent.transform.GetChild(i).gameObject);
        }

        // Build obstracle map
        simulationBound = GameObject.Find("Extent").GetComponent<Renderer>().bounds;
        ObstacleMap obstacleMap = new ObstacleMap(simulationBound, obstracleGo, GRID_SIZE);

        // Planning
        x0 = GameObject.Find("StartingPoint").transform.position;
        x1 = GameObject.Find("EndingPoint").transform.position;
        List<Vector3> path = aStar(x0, x1, simulationBound, obstacleMap);
        var sPath = smoothPath(path, obstacleMap);
        renderPath(sPath);

        // Sent waypoint to robot
        var robot = GameObject.Find("Robot").GetComponent<Robot>();
        robot.SetWayPoint(sPath);
    }

    void renderPath(List<Vector3> path, float width = 0.02f)
    {
        GameObject go = new GameObject("Path");
        go.transform.parent = transform;
        var lr = go.AddComponent<LineRenderer>();
        lr.material = Resources.Load<Material>("Material/Line");
        lr.startColor = Color.white;
        lr.positionCount = path.Count;
        lr.SetPositions(path.ToArray());
        lr.startWidth = width;
        lr.endWidth = width;
        lr.useWorldSpace = false;

        GameObject cps = new GameObject("ControlPoints");
        cps.transform.parent = go.transform;

        for(int i=1; i<path.Count-1; ++i)
        {
            GameObject cp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cp.name = i.ToString();
            cp.transform.localScale = Vector3.one * width * 10;
            cp.transform.position = path[i];
            cp.transform.parent = cps.transform;
            var renderer = cp.GetComponent<Renderer>();
            renderer.material = Resources.Load<Material>("Material/ControlPoint");
        }
        go.transform.Translate(Vector3.up * 0.1f);
    }

    List<Vector3> smoothPath(List<Vector3> original, ObstacleMap obstacleMap)
    {
        List<Vector3> output = new List<Vector3>() { original[0] };

        for(int i=0; i<original.Count - 1; ++i)
        {
            Vector3 start = original[i];
            int newEndIndex = i + 1;
            for (int j=newEndIndex; j<original.Count; ++j)
            {
                Vector3 end = original[j];
                if (!obstacleMap.IsPathCollide(start, end))
                {
                    newEndIndex = j;
                }
                else
                {
                    break;
                }
            }
            output.Add(original[newEndIndex]);
            i = newEndIndex;
        }

        return output;
    }

    int getKey(Vector3 v)
    {
        return v.GetHashCode();
    }

    float heuristic(Vector3 a, Vector3 b)
    {
        return Vector3.Magnitude(b - a);
    }

    List<Vector3> findNeighbor(Vector3 pos, Bounds b)
    {
        List<Vector3> output = new List<Vector3>();
        // up
        Vector3 v = pos + new Vector3(0, 0, GRID_SIZE);
        if (b.Contains(v))
            output.Add(v);

        // down
        v = pos + new Vector3(0, 0, -GRID_SIZE);
        if (b.Contains(v))
            output.Add(v);

        // left
        v = pos + new Vector3(-GRID_SIZE, 0, 0);
        if (b.Contains(v))
            output.Add(v);

        // right
        v = pos + new Vector3(GRID_SIZE, 0, 0);
        if (b.Contains(v))
            output.Add(v);

        return output;
    }

    List<Vector3> reconstructPath(Dictionary<int, int> cameFrom, int currentId, Dictionary<int, Vector3> allPos)
    {
        List<Vector3> totalPath = new List<Vector3>
        {
            allPos[currentId]
        };
        while (cameFrom.ContainsKey(currentId))
        {
            currentId = cameFrom[currentId];
            totalPath.Add(allPos[currentId]);
        }
        return totalPath;
    }

    List<Vector3> aStar(Vector3 start, Vector3 goal, Bounds bound, ObstacleMap obstacleMap)
    {

        Dictionary<int, Vector3> allPos = new Dictionary<int, Vector3>();
        allPos[getKey(start)] = start;
        allPos[getKey(goal)] = goal;

        PriorityQueue<int, float> openSet = new PriorityQueue<int, float>();
        openSet.Enqueue(getKey(start), heuristic(start, goal));
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        Dictionary<int, float> gScore = new Dictionary<int, float>();
        gScore[getKey(start)] = 0;

        while (openSet.Count > 0)
        {
            var currentId = openSet.Peek();
            if (Vector3.Magnitude(goal - allPos[currentId]) < GRID_SIZE)
            {
                Debug.Log("Found");
                return reconstructPath(cameFrom, currentId, allPos);
            }

            openSet.Dequeue();
            List<Vector3> neighbors = findNeighbor(allPos[currentId], bound);
            foreach (var n in neighbors)
            {
                int neighborKey = getKey(n);
                if (!allPos.ContainsKey(neighborKey))
                    allPos[neighborKey] = n;

                float tentativeScore = gScore[currentId] + GRID_SIZE;
                // check collision with obstracle
                if (obstacleMap.IsCollide(n))
                {
                    tentativeScore = float.MaxValue;
                }

                if (!gScore.ContainsKey(neighborKey) || tentativeScore < gScore[neighborKey])
                {
                    cameFrom[neighborKey] = currentId;
                    gScore[neighborKey] = tentativeScore;
                    openSet.Enqueue(neighborKey, tentativeScore + heuristic(n, goal));
                }
            }
        }

        Debug.Log("Not found");
        return new List<Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        Debug.DrawLine(x0, x1, Color.blue, 2.0f);

        for(int i=0; i<pathToRender.Count -1; ++i)
        {
            Debug.DrawLine(pathToRender[i], pathToRender[i + 1], Color.green);
        }
    }
}
