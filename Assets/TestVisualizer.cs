using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class TestVisualizer : MonoBehaviour
{
    public Vector3 Pos0 = new Vector3(0, 0, 0);
    public Vector3 Pos1 = new Vector3(0, 0, 0);
    public bool ShowVelocities = true;
    public Vector3 Vel0 = new Vector3(1, 0, 0);
    public Vector3 Vel1 = new Vector3(0, 1, 0);
    public float GroundClearance = 1f;
    public float obstacleMargin = 0.1f;
    private Vector3 topCollisionPoint;
    private Vector3 relX;
    private Vector3 relY;
    public List<(Vector3, Vector3, Color)> displayVectors = new List<(Vector3, Vector3, Color)>();
    public FootPath footPath;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        displayVectors.Clear();

        if (ShowVelocities)
        {
            Vel0 = Vel0.normalized;
            Vel1 = Vel1.normalized;
        }

        Vector3 diff = Pos1 - Pos0;
        displayVectors.Add((Pos0, Pos1, Color.yellow));

        Vector3 cross = Vector3.Cross(diff, Vector3.up);
        displayVectors.Add((Pos0, Pos0 + cross, Color.blue));

        relY = Vector3.Cross(cross, diff).normalized;
        displayVectors.Add((Pos0 + relY, Pos0, Color.cyan));

        relX = diff.normalized;

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

                displayVectors.Add((Pos0 + relY * y, newHit ? hitInfo.point : Pos1 + relY * y, newHit ? Color.red : Color.green));

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

                displayVectors.Add((Pos0 + relY * y, newHit ? hitInfo.point : Pos1 + relY * y, newHit ? Color.red : Color.green));

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

        topCollisionPoint = BinaryHitScan(0, 0, true);

        footPath = new FootPath(Pos0 + new Vector3(0.3f,0,0), Pos1 + new Vector3(0.3f,0,0));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(Pos0, 0.1f);
        if (ShowVelocities) Gizmos.DrawLine(Pos0, Pos0 + Vel0);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(Pos1, 0.1f);
        if (ShowVelocities) Gizmos.DrawLine(Pos1, Pos1 + Vel1);

        Gizmos.DrawSphere(topCollisionPoint, 0.03f);

        // foreach (var (start, end, color) in displayVectors)
        // {
        //     Gizmos.color = color;
        //     Gizmos.DrawLine(start, end);
        // }

        Vector3 pt = topCollisionPoint + relY * obstacleMargin;
        float controlY = Vector3.Dot(topCollisionPoint - Pos0, relY);
        // float controlX = Vector3.Dot(pt - Pos0, relX);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(Pos0 + Vector3.up * controlY, 0.05f);
        // Gizmos.DrawSphere(pt + relX * -controlX, 0.05f);
        Gizmos.DrawSphere(pt, 0.05f);
        // Gizmos.DrawSphere(pt + relX * controlX, 0.05f);
        Gizmos.DrawSphere(Pos1 + Vector3.up * controlY, 0.05f);
        Gizmos.color = Color.black;
        Gizmos.DrawLineStrip(new Vector3[]
        {
            Pos0,
            Pos0 + Vector3.up * controlY,
            // pt + relX * -controlX,
            pt,
            // pt + relX * controlX,
            Pos1 + Vector3.up * controlY,
            Pos1
        }, false);

        for (float i = 0; i < 1; i += 0.1f)
        {
            // Vector3 a = Vector3.Lerp(
            //     Vector3.Lerp(
            //         Vector3.Lerp(Pos0, Pos0 + Vector3.up * controlY, i),
            //         Vector3.Lerp(Pos0 + Vector3.up * controlY, pt + relX * -controlX, i),
            //         i),
            //     Vector3.Lerp(
            //         Vector3.Lerp(Pos0 + Vector3.up * controlY, pt + relX * -controlX, i),
            //         Vector3.Lerp(pt + relX * -controlX, pt, i),
            //         i),
            //     i
            // );

            // Vector3 b = Vector3.Lerp(
            //     Vector3.Lerp(
            //         Vector3.Lerp(Pos0, Pos0 + Vector3.up * controlY, i + 0.1f),
            //         Vector3.Lerp(Pos0 + Vector3.up * controlY, pt + relX * -controlX, i + 0.1f),
            //         i + 0.1f),
            //     Vector3.Lerp(
            //         Vector3.Lerp(Pos0 + Vector3.up * controlY, pt + relX * -controlX, i + 0.1f),
            //         Vector3.Lerp(pt + relX * -controlX, pt, i + 0.1f),
            //         i + 0.1f),
            //     i + 0.1f
            // );

            Vector3 a = Vector3.Lerp(
                Vector3.Lerp(Pos0, Pos0 + Vector3.up * controlY, i),
                Vector3.Lerp(Pos0 + Vector3.up * controlY, pt, i),
                i
            );

            Vector3 b = Vector3.Lerp(
                Vector3.Lerp(Pos0, Pos0 + Vector3.up * controlY, i + 0.1f),
                Vector3.Lerp(Pos0 + Vector3.up * controlY, pt, i + 0.1f),
                i + 0.1f
            );

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(a, b);
        }
        for (float i = 0; i < 1; i += 0.1f)
        {
            // Vector3 a = Vector3.Lerp(
            //     Vector3.Lerp(
            //         Vector3.Lerp(pt, pt + relX * (1-controlX), i),
            //         Vector3.Lerp(pt + relX * (1-controlX), Pos1 + Vector3.up * controlY, i),
            //         i),
            //     Vector3.Lerp(
            //         Vector3.Lerp(pt + relX * (1-controlX), Pos1 + Vector3.up * controlY, i),
            //         Vector3.Lerp(Pos1 + Vector3.up * controlY, Pos1, i),
            //         i),
            //     i
            // );

            // Vector3 b = Vector3.Lerp(
            //     Vector3.Lerp(
            //         Vector3.Lerp(pt, pt + relX * (1-controlX), i + 0.1f),
            //         Vector3.Lerp(pt + relX * (1-controlX), Pos1 + Vector3.up * controlY, i + 0.1f),
            //         i + 0.1f),
            //     Vector3.Lerp(
            //         Vector3.Lerp(pt + relX * (1-controlX), Pos1 + Vector3.up * controlY, i + 0.1f),
            //         Vector3.Lerp(Pos1 + Vector3.up * controlY, Pos1, i + 0.1f),
            //         i + 0.1f),
            //     i + 0.1f
            // );

            Vector3 a = Vector3.Lerp(
                Vector3.Lerp(pt, Pos1 + Vector3.up * controlY, i),
                Vector3.Lerp(Pos1 + Vector3.up * controlY, Pos1, i),
                i
            );

            Vector3 b = Vector3.Lerp(
                Vector3.Lerp(pt, Pos1 + Vector3.up * controlY, i + 0.1f),
                Vector3.Lerp(Pos1 + Vector3.up * controlY, Pos1, i + 0.1f),
                i + 0.1f
            );
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(a, b);
        }
        footPath.Draw();
    }
}

[CustomEditor(typeof(TestVisualizer))]
public class TestVisualizerEditor : Editor
{
    void OnSceneGUI()
    {
        TestVisualizer test = (TestVisualizer)target;

        EditorGUI.BeginChangeCheck();
        Vector3 p0 = Handles.PositionHandle(test.Pos0, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(test, "Change p1 position");
            test.Pos0 = p0;
            EditorUtility.SetDirty(test);
        }

        if (test.ShowVelocities)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 v0 = Handles.PositionHandle(test.Pos0 + test.Vel0, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(test, "Change v0 position");
                test.Vel0 = v0 - test.Pos0;
                EditorUtility.SetDirty(test);
            }
        }

        EditorGUI.BeginChangeCheck();
        Vector3 p1 = Handles.PositionHandle(test.Pos1, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(test, "Change p1 position");
            test.Pos1 = p1;
            EditorUtility.SetDirty(test);
        }

        if (test.ShowVelocities)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 v1 = Handles.PositionHandle(test.Pos1 + test.Vel1, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(test, "Change v1 position");
                test.Vel1 = v1 - test.Pos1;
                EditorUtility.SetDirty(test);
            }
        }
    }
}

