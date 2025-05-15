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
    private Vector3[] moveTargets = new Vector3[LEG_COUNT];
    private bool[] grounded = new bool[LEG_COUNT];
    public Vector3 pathTarget = new Vector3(0, 0, 0);
    [SerializeField] private float pathTargetRadius = 0.5f;
    [SerializeField] private float mechVelocity = 0.5f;
    [SerializeField] private State state = State.Idle;
    public bool edit = false;
    [Header("Gizmos")]
    [SerializeField] private bool showRestTargets = false;
    [SerializeField] private bool showMoveTargets = false;

    private Vector3 mechDirection = new(0f,0f,0f);

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float distanceToPT = (pathTarget - transform.position).magnitude;
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
            float distance = ((restTargets[i]  + transform.position) - feetPositions[i]).magnitude;
            if (distance > targetWidth || !grounded[i])
            {
                if (grounded[i])
                {
                    grounded[i] = !(grounded[(i + LEG_COUNT - 1) % LEG_COUNT] && grounded[(i + LEG_COUNT + 1) % LEG_COUNT]);

                    if (!grounded[i])
                    {
                        Vector3 heightlessTarget = restTargets[i] + transform.position;
                    
                        Vector3 overshoot = Vector3.zero;
                        if (state != State.Idle) overshoot = mechDirection * targetOvershoot * targetWidth;
                        // Implement hole detection
                        RaycastHit hit;
                        if (Physics.Raycast(
                            heightlessTarget +
                            overshoot +
                            Vector3.up * 10,
                            Vector3.down, out hit, 20))
                        {
                            moveTargets[i] = hit.point;
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
                    }
                }
            }
            else
            {
                grounded[i] = true;
            }
        }
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
                DrawCircle(target + Vector3.Scale(transform.position, new (1,0,1)), targetWidth, Color.red);
            }

        for (int i = 0; i < LEG_COUNT; i++)
        {
            Gizmos.color = grounded[i]? Color.blue : Color.cyan;
            Gizmos.DrawSphere(feetPositions[i], 0.2f);

            if (!showMoveTargets) continue;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(restTargets[i] + transform.position, feetPositions[i]);
            Gizmos.DrawSphere(moveTargets[i], 0.1f);
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