using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class FootPath
{
    public Vector3 Pos0 { get; private set; }
    public Vector3 Pos1 { get; private set; }
    public Vector3 Pos2 { get; private set; }
    private float length => (Pos2 - Pos0).magnitude;
    private float progress;
    private float curveJoint;
    private Vector3 relX;
    private Vector3 relY;
    private float climb;
    public static float GroundClearance = 1f;
    public static float ObstacleMargin = 0.1f;
    public Vector3 tp; // Top collision point

    // public List<(Vector3, Vector3, Color)> displayVectors = new List<(Vector3, Vector3, Color)>();

    public FootPath(Vector3 currentPos, Vector3 destinationPos)
    {
        Pos0 = currentPos;
        Pos2 = destinationPos;

        // Straight path
        Vector3 diff = Pos2 - Pos0;

        // Perpendicular vector
        Vector3 cross = Vector3.Cross(diff, Vector3.up);

        // Normalized straight path
        relX = diff.normalized;

        // Relative up vector
        relY = Vector3.Cross(cross, diff).normalized;

        Vector3 BinaryHitScan(int iteration, float y, bool hit)
        {
            // Optimize by combining y with iteration as an int
            iteration++;
            if (hit)
            {
                y += GroundClearance * Mathf.Pow(0.5f, iteration);
                RaycastHit hitInfo;
                bool newHit = Physics.Raycast(
                    Pos0 + relY * y,
                    relX,
                    out hitInfo,
                    length
                    );

                // displayVectors.Add((Pos0 + relY * y, newHit ? hitInfo.point : Pos2 + relY * y, newHit ? Color.red : Color.green));

                if (iteration > 5)
                {
                    return hitInfo.point;
                }
                else
                {
                    Vector3 p = BinaryHitScan(iteration, y, newHit);
                    return p == Vector3.zero ? hitInfo.point : p;
                    // return p != Vector3.zero ? hitInfo.point : Pos0 + relX * (diff.magnitude / 2) + relY * GroundClearance;
                }
            }
            else
            {
                y -= GroundClearance * Mathf.Pow(0.5f, iteration);
                RaycastHit hitInfo;
                bool newHit = Physics.Raycast(
                    Pos0 + relY * y,
                    relX,
                    out hitInfo,
                    length
                    );

                // displayVectors.Add((Pos0 + relY * y, newHit ? hitInfo.point : Pos2 + relY * y, newHit ? Color.red : Color.green));

                if (iteration > 5)
                {
                    return hitInfo.point;
                }
                else
                {
                    Vector3 p = BinaryHitScan(iteration, y, newHit);
                    return p == Vector3.zero ? hitInfo.point : p;
                    // return p != Vector3.zero ? hitInfo.point : Pos0 + relX * (diff.magnitude / 2) + relY * GroundClearance;
                }
            }
        }

        Vector3 topCollisionPoint = BinaryHitScan(0, 0, true);
        if (topCollisionPoint == Vector3.zero)
        {
            topCollisionPoint = (Pos0 + Pos2) * 0.5f + relY * (length / 3);
        }

        tp = topCollisionPoint;

        // Middle point of curve
        Pos1 = topCollisionPoint + relY * ObstacleMargin;

        // Relative vertical climb
        climb = Vector3.Dot(topCollisionPoint - Pos0, relY);

        // At what input t interpolation should switch from one curve to the next
        curveJoint = Vector3.Dot(Pos1 - Pos0, relX) / length;
    }

    public Vector3 GetPosition(float t)
    {
        if (t < curveJoint)
        {
            return Vector3.Lerp(
                Vector3.Lerp(Pos0, Pos0 + Vector3.up * climb, t / curveJoint),
                Vector3.Lerp(Pos0 + Vector3.up * climb, Pos1, t / curveJoint),
                t / curveJoint
            );
        }

        Vector3 val =
        Vector3.Lerp(
            Vector3.Lerp(Pos1, Pos2 + Vector3.up * climb, (t - curveJoint) / (1 - curveJoint)),
            Vector3.Lerp(Pos2 + Vector3.up * climb, Pos2, (t - curveJoint) / (1 - curveJoint)),
            (t - curveJoint) / (1 - curveJoint)
        );
        return val;
    }

    public Vector3 Move(float velocity, out bool finished)
    {
        finished = false;
        progress += velocity * Time.deltaTime;
        if (progress >= length)
        {
            progress = length;
            finished = true;
            return Pos2;
        }

        if (length == 0)
        {
            Debug.LogWarning("Length is zero");
        }
        float t = progress / length;
        return GetPosition(t);
    }

    public void Draw()
    {
        for (float t = 0; t < 1; t += 0.05f)
        {
            Vector3 a = GetPosition(t);
            Vector3 b = GetPosition(t + 0.05f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(a, b);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(Pos0, 0.03f);
        Gizmos.DrawSphere(Pos2, 0.03f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(tp, 0.03f);
    }
}