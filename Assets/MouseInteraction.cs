

using UnityEngine;

public class MouseInteraction : MonoBehaviour
{

    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
    PathFinder pathFinder;

    bool firstClicked = false;

    void Start()
    {
        pathFinder = GameObject.FindObjectOfType<PathFinder>();
    }

    // Update is called once per frame
    void Update()
    {
        bool leftClick = Input.GetMouseButtonDown(0);
        if (leftClick)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float enter = 0.0f;

            if (groundPlane.Raycast(ray, out enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                if (!firstClicked)
                {
                    pathFinder.SetStartingPoint(hitPoint);
                }
                else
                {
                    pathFinder.SetEndingPoint(hitPoint);
                }
                firstClicked = !firstClicked;
                
            }
        }
    }
}
