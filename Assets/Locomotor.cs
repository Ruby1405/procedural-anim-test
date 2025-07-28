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
    public Foot[] feet = new Foot[LEG_COUNT];
    public bool edit = false;
    [SerializeField] private float maxAltitudeDeviation = 1.0f;
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

    void Awake()
    {
        Foot.maxAltitudeDeviation = maxAltitudeDeviation;
        Foot.targetWidth = targetWidth;
        Foot.idleTargetWidth = idleTargetWidth;
        Foot.walkingTargetWidth = walkingTargetWidth;
        Foot.targetOvershoot = targetOvershoot;
        Foot.velocity = footVelocity;
        
        feet[0] = new Foot(new Vector3(0.5f, 0, 0.5f));
        feet[1] = new Foot(new Vector3(0.5f, 0, -0.5f));
        feet[2] = new Foot(new Vector3(-0.5f, 0, 0.5f));
        feet[3] = new Foot(new Vector3(-0.5f, 0, -0.5f));
        feet[4] = new Foot(new Vector3(0, 0, 0.5f));
        feet[5] = new Foot(new Vector3(0, 0, -0.5f));
    }

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
            feet[i].Update(
                feet[(i + LEG_COUNT - 1) % LEG_COUNT].Grounded,
                feet[(i + 1) % LEG_COUNT].Grounded,
                transform.position,
                state,
                mechDirection
            );
        }

        short groundedCount = 0;
        float altitudeSum = 0;
        for (int i = 0; i < LEG_COUNT; i++)
        {
            if (!feet[i].Grounded) continue;
            groundedCount++;
            altitudeSum += feet[i].Position.y;
        }
        transform.position = new Vector3(
            transform.position.x,
            altitudeSum / groundedCount + coreHeight,
            transform.position.z
        );
    }

    public static void DrawCircle(Vector3 center, float radius, Color color)
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

        foreach (var foot in feet)
        {
            foot.Draw(
                transform.position,
                state,
                mechDirection,
                showMoveTargets,
                showRestTargets);
        }
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
            Vector3 target = Handles.PositionHandle(loc.feet[i].restTarget + Vector3.Scale(loc.transform.position, new(1, 0, 1)), Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(loc, "Change Target Position");
                loc.feet[i].restTarget = target - Vector3.Scale(loc.transform.position, new(1, 0, 1));
                EditorUtility.SetDirty(loc);
            }
        }
    }
}