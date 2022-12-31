using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robot : MonoBehaviour
{
    public enum ControlType { Waypoint, Emulation };
    public enum DriveMode { Forward, SpotTurn, Curve };

    const float MAX_ANGLE_DIFF = 3.0f;     // degree

    // path
    List<Vector3> waypoint = new List<Vector3>();

    // linear
    float currentVelocity = 0.0f;
    float maxAcceleration = 3.0f;
    float maxDeceleration = -3.0f;
    float maxVelocity = 20.0f;

    // rotation
    float rotCurrentVelocity = 0.0f;
    float rotMaxVelocity = 120.0f;
    float rotAcceleration = 40.0f;

    // state
    int currentIndex = 0;
    DriveMode currentDriveMode = DriveMode.Forward;
    ControlType controlType = ControlType.Waypoint;

    public void SetWayPoint (List<Vector3> _waypoint, ControlType _controlType)
    {
        controlType = _controlType;
        waypoint = _waypoint;
        currentIndex = 0;
        currentVelocity = 0.0f;
        rotCurrentVelocity = 0.0f;
        currentDriveMode = DriveMode.Forward;

        transform.position = waypoint[0];
        if (waypoint.Count >= 2)
            transform.forward = waypoint[1] - waypoint[0];
    }

    void WayPointControlLoop()
    {
        if (waypoint.Count == 0 || currentIndex >= waypoint.Count - 1)
            return;

        Vector3 pos = transform.position;
        Vector3 dir = transform.forward;
        float dt = Time.deltaTime;

        // SPOTTURN
        if (currentDriveMode == DriveMode.SpotTurn)
        {
            Vector3 newForward = waypoint[currentIndex + 1] - pos;
            float angleDiff = Vector3.SignedAngle(transform.forward, newForward, Vector3.up);  // left hand rule

            if (Mathf.Abs(angleDiff) > MAX_ANGLE_DIFF)
            {
                float stopingTheta = -rotCurrentVelocity * rotCurrentVelocity / -rotAcceleration / 2;
                float rotAccel;
                float direction = angleDiff < 0 ? 1.0f : -1.0f;

                if (Mathf.Abs(angleDiff) > stopingTheta)
                    rotAccel = direction * -rotAcceleration;
                else
                    rotAccel = direction * rotAcceleration;

                rotCurrentVelocity += rotAccel * dt;
                if (Mathf.Abs(rotCurrentVelocity) > rotMaxVelocity)
                    rotCurrentVelocity = direction * rotMaxVelocity;

                transform.Rotate(Vector3.up, rotCurrentVelocity * dt);
                return;
            }
            else
            {
                ChangeDriveMode(DriveMode.Forward);
            }
        }

        // FORWARD DRIVING
        else if (currentDriveMode == DriveMode.Forward)
        {

            // linear motion
            Vector3 v1_pos = waypoint[currentIndex + 1] - pos;

            // next waypoint has been reached
            if (Vector3.Dot(dir, v1_pos) < 0)
            {
                currentIndex += 1;
                ChangeDriveMode(DriveMode.SpotTurn);
                return;
            }

            // find stopping distance
            float stopingDistance = -currentVelocity * currentVelocity / maxDeceleration / 2;

            // integrate accel to get velocity
            float accel;
            if (Vector3.Magnitude(v1_pos) < stopingDistance)
                accel = maxDeceleration;
            else
                accel = maxAcceleration;
            currentVelocity += accel * dt;

            if (currentVelocity < 0)
                currentVelocity = 0;
            else if (currentVelocity > maxVelocity)
                currentVelocity = maxVelocity;

            pos += transform.forward * currentVelocity * dt;
            transform.position = pos;
        }
    }

    void EmulationControlLoop()
    {

    }

    void Update()
    {
        if (controlType == ControlType.Waypoint)
        {
            WayPointControlLoop();
        }
        else if (controlType == ControlType.Emulation)
        {
            EmulationControlLoop();
        }
    }

    void ChangeDriveMode(DriveMode newMode)
    {
        // Forward > SpotTurn
        if (currentDriveMode == DriveMode.Forward && newMode == DriveMode.SpotTurn)
        {
            rotCurrentVelocity = 0.0f;
        }

        // Forward
        if (newMode == DriveMode.Forward)
        {
            currentVelocity = 0.0f;
        }

        currentDriveMode = newMode;
    }
}
