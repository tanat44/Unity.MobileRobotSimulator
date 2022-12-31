using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    Vector3 x0, x1;
    Bounds simulationBound;
    ObstacleMap obstacleMap;
    GameObject x0GameObject, x1GameObject;
    GameObject pathGameObject;

    const float GRID_SIZE = 0.2f;
    public const float OBSTACLE_SAFE_DISTANCE = 0.5f;

    void Start()
    {
        // Init
        pathGameObject = new GameObject("Path");

        // Find all obstracles
        var obstracleParent = GameObject.Find("Obstracle");
        List<GameObject> obstracleGo = new List<GameObject>();
        var safeZoneMaterial = Resources.Load<Material>("Material/ObstacleSafeZone");
        var safeZoneParent = new GameObject("SafeZone");
        for(int i=0; i<obstracleParent.transform.childCount; ++i)
        {
            GameObject go = obstracleParent.transform.GetChild(i).gameObject;
            obstracleGo.Add(go);

            // Draw safezone
            GameObject safeZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safeZone.transform.parent = safeZoneParent.transform;
            safeZone.name = go.name + "-SafeZone";
            safeZone.transform.position = go.transform.position + Vector3.up * 0.1f;
            var scale = go.transform.localScale;
            safeZone.transform.localScale = new Vector3(scale.x + 2*OBSTACLE_SAFE_DISTANCE, 0.1f, scale.z + 2 * OBSTACLE_SAFE_DISTANCE);
            var renderer = safeZone.GetComponent<Renderer>();
            renderer.material = safeZoneMaterial;

        }

        // Build obstracle map
        simulationBound = GameObject.Find("Extent").GetComponent<Renderer>().bounds;
        obstacleMap = new ObstacleMap(simulationBound, obstracleGo, GRID_SIZE);

        // Init startingPoint and endingPoint
        x0GameObject = GameObject.Find("StartingPoint");
        x0 = x0GameObject.transform.position;
        x1GameObject = GameObject.Find("EndingPoint");
        x1 = x1GameObject.transform.position;

        RecalculatePath();
    }

    public void RecalculatePath()
    {
        List<Vector3> path = aStar(x0, x1, simulationBound, obstacleMap);
        var sPath = smoothPath(path, obstacleMap);
        renderPath(sPath, Color.cyan, "SmoothPath");

        // Bezier
        CompositeBezierPath bPath = new CompositeBezierPath(sPath);
        renderPath(bPath.Rasterize(), Color.blue, "CompositeBezier");

        // Sent waypoint to robot
        var robot = GameObject.Find("Robot").GetComponent<Robot>();
        robot.SetWayPoint(bPath.Rasterize(), Robot.ControlType.Waypoint);
    }

    public void SetStartingPoint(Vector3 start)
    {
        x0 = start;
        x0GameObject.transform.position = start;
        RecalculatePath();
    }

    public void SetEndingPoint(Vector3 end)
    {
        x1 = end;
        x1GameObject.transform.position = end;
        RecalculatePath();
    }

    void renderPath(List<Vector3> path, Color color, string name, float width = 0.02f)
    {
        var oldRender = GameObject.Find($"{pathGameObject.name}/{name}");
        if (oldRender != null)
            GameObject.Destroy(oldRender);

        // render polyline
        GameObject go = new GameObject(name);
        go.transform.parent = pathGameObject.transform;
        var lr = go.AddComponent<LineRenderer>();
        var material = Resources.Load<Material>("Material/Line");
        var newMaterial = Instantiate(material);
        newMaterial.color = color;
        lr.material = newMaterial;
        lr.positionCount = path.Count;
        lr.SetPositions(path.ToArray());
        lr.startWidth = width;
        lr.endWidth = width;
        lr.useWorldSpace = false;

        // render controlpoints
        GameObject cps = new GameObject("ControlPoints");
        cps.transform.parent = go.transform;
        var cpMaterial = Instantiate<Material>(newMaterial);
        const float lightness = 0.5f;
        cpMaterial.color = new Color(color.r * lightness, color.g * lightness, color.b * lightness);

        for (int i=1; i<path.Count-1; ++i)
        {
            GameObject cp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cp.name = i.ToString();
            cp.transform.localScale = Vector3.one * width * 4;
            cp.transform.position = path[i];
            cp.transform.parent = cps.transform;
            var renderer = cp.GetComponent<Renderer>();
            renderer.material = cpMaterial;
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
        totalPath.Reverse();
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
                Debug.Log("A* Found Solution");
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

        Debug.Log("A* Not Found");
        return new List<Vector3>();
    }
}
