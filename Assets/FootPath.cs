using System;
using UnityEngine;

[Serializable]
public class FootPath
{
    public Vector3 Pos0 { get; private set; }
    public Vector3 Pos1 { get; private set; }
    public Vector3 Pos2 { get; private set; }
    private float curveJoint;
    private Vector3 relX;
    private Vector3 relY;
    private float climb;
    public static float GroundClearance = 1f;
    public static float ObstacleMargin = 0.1f;

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
                    diff.magnitude
                    );

                // displayVectors.Add((Pos0 + relY * y, newHit ? hitInfo.point : Pos1 + relY * y, newHit ? Color.red : Color.green));

                if (iteration > 5)
                {
                    return hitInfo.point;
                }
                else
                {
                    Vector3 p = BinaryHitScan(iteration, y, newHit);
                    return p == Vector3.zero ? hitInfo.point : p;
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
                    diff.magnitude
                    );

                // displayVectors.Add((Pos0 + relY * y, newHit ? hitInfo.point : Pos1 + relY * y, newHit ? Color.red : Color.green));

                if (iteration > 5)
                {
                    return hitInfo.point;
                }
                else
                {
                    Vector3 p = BinaryHitScan(iteration, y, newHit);
                    return p == Vector3.zero ? hitInfo.point : p;
                }
            }
        }

        Vector3 topCollisionPoint = BinaryHitScan(0, 0, true);

        // Middle point of curve
        Pos1 = topCollisionPoint + relY * ObstacleMargin;

        // Relative vertical climb
        climb = Vector3.Dot(topCollisionPoint - Pos0, relY);

        // At what input t interpolation should switch from one curve to the next
        curveJoint = Vector3.Dot(Pos1 - Pos0, relX);
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
        return Vector3.Lerp(
            Vector3.Lerp(Pos1, Pos2 + Vector3.up * climb, (t - curveJoint) / (1 - curveJoint)),
            Vector3.Lerp(Pos2 + Vector3.up * climb, Pos1, (t - curveJoint) / (1 - curveJoint)),
            (t - curveJoint) / (1 - curveJoint)
        );
    }

    public void Draw()
    {
        for (float t = 0; t < 1; t += 0.1f)
        {
            Vector3 a = Vector3.Lerp(
                Vector3.Lerp(Pos0, Pos0 + Vector3.up * climb, t / curveJoint),
                Vector3.Lerp(Pos0 + Vector3.up * climb, Pos1, t / curveJoint),
                t / curveJoint
            );

            Vector3 b = Vector3.Lerp(
                Vector3.Lerp(Pos0, Pos0 + Vector3.up * climb, t / curveJoint + 0.1f),
                Vector3.Lerp(Pos0 + Vector3.up * climb, Pos1, t / curveJoint + 0.1f),
                t / curveJoint + 0.1f
            );

            Gizmos.color = Color.green;
            Gizmos.DrawLine(a, b);
        }

        for (float t = 0; t < 1; t += 0.1f)
        {
            Vector3 a = Vector3.Lerp(
                Vector3.Lerp(Pos1, Pos2 + Vector3.up * climb, (t - curveJoint) / (1 - curveJoint)),
                Vector3.Lerp(Pos2 + Vector3.up * climb, Pos1, (t - curveJoint) / (1 - curveJoint)),
                (t - curveJoint) / (1 - curveJoint)
            );

            Vector3 b = Vector3.Lerp(
                Vector3.Lerp(Pos1, Pos2 + Vector3.up * climb, (t - curveJoint) / (1 - curveJoint) + 0.1f),
                Vector3.Lerp(Pos2 + Vector3.up * climb, Pos1, (t - curveJoint) / (1 - curveJoint) + 0.1f),
                (t - curveJoint) / (1 - curveJoint) + 0.1f
            );

            Gizmos.color = Color.green;
            Gizmos.DrawLine(a, b);
        }
    }
}