using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class Locomotor : MonoBehaviour
{
    [SerializeField] private float idleTargetWidth = 0.1f;
    [SerializeField] private float walkingTargetWidth = 0.1f;
    [SerializeField] private float targetWidth = 0.1f;
    [SerializeField] private float targetOvershoot = 0.09f;
    [SerializeField] private float footVelocity = 0.1f;
    public const int LEG_COUNT = 6;
    [SerializeField] private Vector3[] feetPositions = new Vector3[LEG_COUNT];
    public Vector3[] idleTargets = new Vector3[LEG_COUNT];
    [SerializeField] private Vector3[] targets = new Vector3[LEG_COUNT];
    [SerializeField] private bool[] grounded = new bool[LEG_COUNT];
    [SerializeField] private State state = State.Idle;
    public bool edit = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case State.Idle:
                targetWidth = idleTargetWidth;
                break;
            case State.Walking:
                targetWidth = walkingTargetWidth;
                break;
            case State.Running:
                targetWidth = walkingTargetWidth;
                break;
            case State.Jumping:
                targetWidth = walkingTargetWidth;
                break;
        }

        if (state == State.Idle)
        {
            for (int i = 0; i < LEG_COUNT; i++)
            {
                Vector3 heightLessTarget = idleTargets[i] + transform.position;
                
                // Implement hole detection
                RaycastHit hit;
                if (Physics.Raycast(heightLessTarget + Vector3.up * 10, Vector3.down, out hit, 20))
                {
                    targets[i] = hit.point;
                }
            }
        }

        for (int i = 0; i < LEG_COUNT; i++)
        {
            float distance = (targets[i] - feetPositions[i]).magnitude;
            if (distance > targetWidth)
            {
                bool OthersGrounded = true;
                for (int ii = 0; ii < LEG_COUNT; ii++)
                {
                    if (i != ii && !grounded[ii])
                    {
                        OthersGrounded = false;
                        break;
                    }
                }
                if (OthersGrounded)
                {
                    grounded[i] = false;
                    feetPositions[i] += (targets[i] - feetPositions[i]).normalized * footVelocity * Time.deltaTime;
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
        foreach (var footTarget in targets)
        {
            DrawCircle(footTarget, targetWidth, Color.red);
        }
        for (int i = 0; i < LEG_COUNT; i++)
        {
            Gizmos.color = grounded[i]? Color.blue : Color.yellow;
            Gizmos.DrawSphere(feetPositions[i], 0.05f);
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
        if (!loc.edit) return;

        for (int i = 0; i < Locomotor.LEG_COUNT; i++)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 target = Handles.PositionHandle(loc.idleTargets[i]  + Vector3.Scale(loc.transform.position, new (1,0,1)), Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(loc, "Change Target Position");
                loc.idleTargets[i] = target - Vector3.Scale(loc.transform.position, new (1,0,1));
                EditorUtility.SetDirty(loc);
            }
        }
    }
}