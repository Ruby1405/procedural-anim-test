using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class Locomotor : MonoBehaviour
{
    [SerializeField] private float idleTargetWidth = 0.1f;
    [SerializeField] private float walkingTargetWidth = 0.1f;
    [SerializeField] private float targetWidth = 0.1f;
    [Range(0,1)][SerializeField] private float targetOvershoot = 0.09f;
    [SerializeField] private float footVelocity = 0.1f;
    public const int LEG_COUNT = 6;
    private Vector3[] feetPositions = new Vector3[LEG_COUNT];
    public Vector3[] restTargets = new Vector3[LEG_COUNT];
    public bool edit = false;
    private Vector3[] moveTargets = new Vector3[LEG_COUNT];
    [SerializeField] private float maxAltitudeDeviation = 1.0f;
    private bool[] grounded = new bool[LEG_COUNT];

    [SerializeField] private float coreHeight = 0.5f;

    [Header("Pathfinding")]
    public Vector3 pathTarget = new Vector3(0, 0, 0);
    [SerializeField] private float pathTargetRadius = 0.5f;
    [SerializeField] private float mechVelocity = 0.5f;
    [SerializeField] private State state = State.Idle;

    [Header("Gizmos")]
    [SerializeField] private bool showRestTargets = false;
    [SerializeField] private bool showMoveTargets = false;
    // [SerializeField] private List<Vector3> traceHits = new List<Vector3>();
    // [SerializeField] private List<Vector3> traceOrigins = new List<Vector3>();

    private Vector3 mechDirection = new(0f, 0f, 0f);

    void Update()
    {
        float distanceToPT = new Vector3(
            pathTarget.x - transform.position.x,
            0f,
            pathTarget.z - transform.position.z
            ).magnitude;

        if (distanceToPT > pathTargetRadius) state = State.Walking;
        else state = State.Idle;

        switch (state)
        {
            case State.Idle:
                targetWidth = idleTargetWidth;
                break;
            case State.Walking:
                targetWidth = walkingTargetWidth;
                mechDirection = (pathTarget - transform.position).normalized;
                transform.position += mechDirection * mechVelocity * Time.deltaTime;
                break;
            case State.Running:
                targetWidth = walkingTargetWidth;
                break;
            case State.Jumping:
                targetWidth = walkingTargetWidth;
                break;
        }

        for (int i = 0; i < LEG_COUNT; i++)
        {
            float distance = new Vector3(
                restTargets[i].x + transform.position.x - feetPositions[i].x,
                restTargets[i].y - feetPositions[i].y,
                restTargets[i].z + transform.position.z - feetPositions[i].z
                ).magnitude;
            if (distance > targetWidth || !grounded[i])
            {
                if (grounded[i])
                {
                    grounded[i] = !(grounded[(i + LEG_COUNT - 1) % LEG_COUNT] && grounded[(i + LEG_COUNT + 1) % LEG_COUNT]);

                    if (!grounded[i])
                    {
                        Vector3 heightlessTarget = restTargets[i] + Vector3.Scale(transform.position, new(1, 0, 1));

                        Vector3 overshoot = Vector3.zero;
                        if (state != State.Idle) overshoot = Vector3.Scale(mechDirection, new(1,0,1)) * targetOvershoot * targetWidth;
                        // Implement hole detection
                        RaycastHit hit;
                        if (Physics.Raycast(
                            heightlessTarget +
                            overshoot +
                            Vector3.up * maxAltitudeDeviation,
                            Vector3.down, out hit, maxAltitudeDeviation * 2))
                        {
                            moveTargets[i] = hit.point;
                            // traceHits.Add(hit.point);
                            // traceOrigins.Add(heightlessTarget + overshoot + Vector3.up * maxAltitudeDeviation);
                        }
                    }
                }

                if (!grounded[i])
                {
                    float stepDistance = footVelocity * Time.deltaTime;
                    Vector3 displacementVector = moveTargets[i] - feetPositions[i];
                    if (displacementVector.magnitude > stepDistance)
                    {
                        feetPositions[i] += displacementVector.normalized * stepDistance;
                    }
                    else
                    {
                        feetPositions[i] = moveTargets[i];
                        grounded[i] = true;
                        restTargets[i].y = feetPositions[i].y;
                    }
                }
            }
            else
            {
                grounded[i] = true;
            }
        }

        short groundedCount = 0;
        float altitudeSum = 0;
        for (int i = 0; i < LEG_COUNT; i++)
        {
            if (!grounded[i]) continue;
            groundedCount++;
            altitudeSum += feetPositions[i].y;
        }
        transform.position = new Vector3(
            transform.position.x,
            altitudeSum / groundedCount + coreHeight,
            transform.position.z
        );
    }

    void DrawCircle(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        for (int i = 0; i < radius * 120; i++)
        {
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(i * Mathf.PI / 6) * radius, 0, Mathf.Sin(i * Mathf.PI / 6) * radius),
                center + new Vector3(Mathf.Cos((i + 1) * Mathf.PI / 6) * radius, 0, Mathf.Sin((i + 1) * Mathf.PI / 6) * radius)
            );
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        DrawCircle(pathTarget, pathTargetRadius, Color.magenta);
        Gizmos.DrawSphere(transform.position, 0.2f);

        if (showRestTargets)
            foreach (var target in restTargets)
            {
                DrawCircle(target + Vector3.Scale(transform.position, new(1, 0, 1)), targetWidth, Color.red);
            }

        for (int i = 0; i < LEG_COUNT; i++)
        {
            Gizmos.color = grounded[i] ? Color.blue : Color.cyan;
            Gizmos.DrawSphere(feetPositions[i], 0.2f);

            if (!showMoveTargets) continue;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(restTargets[i] + Vector3.Scale(transform.position, new(1, 0, 1)), feetPositions[i]);
            Gizmos.DrawSphere(moveTargets[i], 0.1f);

            // Show ray cast targets
            if (false) continue;
            Gizmos.color = Color.yellow;

            Vector3 heightlessTarget = restTargets[i] + Vector3.Scale(transform.position, new(1, 0, 1));

            Vector3 overshoot = Vector3.zero;
            if (state != State.Idle) overshoot = mechDirection * targetOvershoot * targetWidth;

            Gizmos.DrawLine(
                heightlessTarget +
                overshoot +
                Vector3.up * maxAltitudeDeviation,
                heightlessTarget +
                overshoot +
                Vector3.down * maxAltitudeDeviation
                );
        }
        // foreach (var hit in traceHits)
        // {
        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawSphere(hit, 0.1f);
        // }
        // foreach (var hit in traceOrigins)
        // {
        //     Gizmos.color = new Color(1f, 0.5f, 0);
        //     Gizmos.DrawSphere(hit, 0.1f);
        // }

    }
}

public enum State
{
    Idle,
    Walking,
    Running,
    Jumping
}

[CustomEditor(typeof(Locomotor))]
public class LocomotorEditor : Editor
{
    void OnSceneGUI()
    {
        Locomotor loc = (Locomotor)target;

        EditorGUI.BeginChangeCheck();
        Vector3 pt = Handles.PositionHandle(loc.pathTarget, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(loc, "Change path target position");
            loc.pathTarget = pt;
            EditorUtility.SetDirty(loc);
        }

        if (!loc.edit) return;

        for (int i = 0; i < Locomotor.LEG_COUNT; i++)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 target = Handles.PositionHandle(loc.restTargets[i] + Vector3.Scale(loc.transform.position, new (1,0,1)), Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(loc, "Change Target Position");
                loc.restTargets[i] = target - Vector3.Scale(loc.transform.position, new (1,0,1));
                EditorUtility.SetDirty(loc);
            }
        }
    }
}