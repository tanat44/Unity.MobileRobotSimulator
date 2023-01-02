using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class Robot : MonoBehaviour
{
    // 3d model
    public GameObject RearWheelLeft;
    public GameObject RearWheelRight;
    
    // path
    List<Vector3> waypoint = new List<Vector3>();

    // linear
    float currentVelocity = 1.0f;
    float maxAcceleration = 3.0f;
    float maxDeceleration = -3.0f;
    float maxVelocity = 0.3f;

    // steering
    float minSteeringThreshold = 10.0f;
    float steeringAngle = 0.0f;
    float steeringMaxAngle = 90.0f;
    float steeringVelocity = 0.0f;
    float steeringMaxVelocity = 60.0f;
    float wheelBaseLength = 1.0f;

    // state
    int targetIndex = 0;
    public void SetWayPoint (List<Vector3> _waypoint )
    {
        waypoint = _waypoint;
        targetIndex = 1;
        currentVelocity = 0.0f;
        steeringAngle = 0.0f;
        steeringVelocity = 0.0f;

        transform.position = waypoint[0];
        if (waypoint.Count >= 2)
            transform.forward = waypoint[1] - waypoint[0];
    }

    void EmulationControlLoop()
    {
        if (waypoint.Count == 0 || targetIndex >= waypoint.Count - 1)
            return;

        Vector3 pos = transform.position;
        Vector3 heading = transform.forward;
        float dt = Time.deltaTime;

        // change the target when the robot go over half distance to the next waypoint
        Vector3 v2_p = waypoint[targetIndex + 1] - pos;
        Vector3 v1_p = waypoint[targetIndex] - pos;
        if (v1_p.magnitude > v2_p.magnitude)
        {
            targetIndex += 1;
            Debug.Log("New Target: " + targetIndex);
            return;
        }

        // (1) STEERING

        // find turning radius
        Vector3 v0 = waypoint[targetIndex] - waypoint[targetIndex - 1];
        Vector3 v1 = waypoint[targetIndex + 1] - waypoint[targetIndex];
        Quaternion rot90 = Quaternion.AngleAxis(90, Vector3.up);
        var turningRadius = float.PositiveInfinity;

        if (Utils.LineLineIntersection(out Vector3 intersection, waypoint[targetIndex - 1] + v0/2, rot90 * v0, waypoint[targetIndex] + v1/2, rot90 *  v1))
        {
            Vector3 center_pos = intersection - pos;
            turningRadius = center_pos.magnitude;
            Vector3 cross = Vector3.Cross(v0, v1);
            float steerDirection = cross.y > 0.0f ? 1.0f : -1.0f;
            var targetAngle = steerDirection * Mathf.Atan(wheelBaseLength / turningRadius) * Mathf.Rad2Deg;

            //// prevent oversteering
            //Vector3 cross_heading_v0 = Vector3.Cross(heading, v1);
            //bool steerBack = false;
            //if (steerDirection > 0.0f && cross_heading_v0.y < 0.0f)
            //    steerBack = true;
            //else if (steerDirection < 0.0f && cross_heading_v0.y > 0.0f)
            //    steerBack = true;
            //if (steerBack)
            //{
            //    targetAngle = Vector3.SignedAngle(Vector3.right, v1, Vector3.down);
            //}

            steeringVelocity = (targetAngle - steeringAngle) / dt;
            if (steeringVelocity > steeringMaxVelocity) 
                steeringVelocity = steeringMaxVelocity;
            else if (steeringVelocity < -steeringMaxVelocity)
                steeringVelocity = -steeringMaxVelocity;
        }
        else
        {
            steeringVelocity = 0;
        }

        steeringAngle += steeringVelocity * dt;
        if (steeringAngle > steeringMaxAngle)
            steeringAngle = steeringMaxAngle;
        else if (steeringAngle < -steeringMaxAngle)
            steeringAngle = -steeringMaxAngle;

        // (2) DRIVE MOTION
        float accel = maxAcceleration;
        currentVelocity += accel * dt;
        if (currentVelocity < 0)
            currentVelocity = 0;
        else if (currentVelocity > maxVelocity)
            currentVelocity = maxVelocity;

        // Ackerman steering
        float angularVelocity = currentVelocity * Mathf.Sin(steeringAngle * Mathf.Deg2Rad) / wheelBaseLength;
        float phi = angularVelocity * Mathf.Rad2Deg * dt ;

        // (3) Update transform
        Vector3 newDirection = Quaternion.AngleAxis(phi, Vector3.up) * Vector3.forward;
        transform.Rotate(Vector3.up, phi);
        float forwardVelocity = currentVelocity * Mathf.Cos(steeringAngle * Mathf.Deg2Rad);
        transform.Translate(newDirection * forwardVelocity * dt);

        // (4) Update 3d model
        if (RearWheelLeft != null)
            RearWheelLeft.transform.localRotation = Quaternion.Euler(0, -steeringAngle, 0);
        if (RearWheelRight != null)
            RearWheelRight.transform.localRotation = Quaternion.Euler(0, -steeringAngle, 0);
    }

    void Update()
    {
        EmulationControlLoop();
    }

}
