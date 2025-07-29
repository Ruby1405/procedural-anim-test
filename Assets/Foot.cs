using System;
using UnityEngine;

[Serializable]
public class Foot
{
    public Vector3 Position;// { get; private set; }
    public Vector3 restTarget;
    private Vector3 moveTarget;
    private FootPath path;
    public static float velocity;
    public static float idleTargetWidth;
    public static float walkingTargetWidth;
    public static float targetOvershoot;
    public static float maxAltitudeDeviation;
    // If the foot is on the ground
    public bool Grounded { get; private set; } = true;

    public Foot(Vector3 restTarget)
    {
        this.restTarget = restTarget;
        Position = restTarget;
        moveTarget = restTarget;
        path = null;
    }
    public void Update(
        bool firstNeighbourGrounded,
        bool secondNeighbourGrounded,
        Vector3 parentPosition,
        float coreHeight,
        State state,
        Vector3 direction
        )
    {
        // Check how far foot is from desired position
        float distance = new Vector3(
            restTarget.x + parentPosition.x - Position.x,
            restTarget.y - Position.y,
            restTarget.z + parentPosition.z - Position.z
        ).magnitude;

        float targetWidth = state switch
        {
            State.Idle => idleTargetWidth,
            State.Walking => walkingTargetWidth,
            _ => walkingTargetWidth
        };

        // If the foot is outside the target and grounded check if it can move
        if (distance > targetWidth && Grounded)
        {
            // If both neighbours are grounded, we can move the foot
            Grounded = !(firstNeighbourGrounded && secondNeighbourGrounded);

            // If the foot is not grounded decide a new target position
            if (!Grounded)
            {
                Vector3 heighlessTarget = restTarget + Vector3.Scale(parentPosition, new(1, 0, 1));

                Vector3 overshoot = Vector3.zero;
                if (state != State.Idle) overshoot = Vector3.Scale(direction, new(1, 0, 1)) * targetOvershoot * targetWidth;

                // Raycast to find the ground below the foot
                // TODO Implement holde detection
                RaycastHit hit;
                if (!Physics.Raycast(
                    heighlessTarget +
                    overshoot +
                    Vector3.up * (maxAltitudeDeviation + parentPosition.y - coreHeight),
                    Vector3.down, out hit, maxAltitudeDeviation * 2))
                {
                    Grounded = true;
                    return;
                }
                moveTarget = hit.point;

                if ((moveTarget - Position).magnitude < idleTargetWidth)
                {
                    Grounded = true;
                    return;
                }

                // create a new path for the foot
                path = new FootPath(Position, moveTarget);
            }
        }

        if (!Grounded)
        {
            if (path is null)
            {
                Debug.LogError("Path is null, cannot update foot position.");
                return;
            }

            // If the foot is not grounded, move it along the path
            Position = path.Move(velocity, out bool finished);
            Grounded = finished;

            // Debug.Log(Position);
        }
    }

    public void Draw(
        Vector3 parentPosition,
        float coreHeight,
        State state,
        Vector3 direction,
        bool showMoveTargets
        )
    {
        Gizmos.color = Grounded ? Color.blue : Color.cyan;
        Gizmos.DrawSphere(Position, 0.2f);

        if (!showMoveTargets) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(restTarget + Vector3.Scale(parentPosition, new(1, 0, 1)), Position);
        Gizmos.DrawSphere(moveTarget, 0.1f);

        if (!Grounded)
        {
            if (path is null)
            {
                Debug.LogError("Path is null, cannot draw foot path.");
                return;
            }

            path.Draw();
        }

        // Show ray cast targets
        Gizmos.color = Color.yellow;

        Vector3 heightlessTarget = restTarget + Vector3.Scale(parentPosition, new(1, 0, 1));

        Vector3 overshoot = Vector3.zero;
        if (state != State.Idle) overshoot = direction * (targetOvershoot * walkingTargetWidth);

        Gizmos.DrawLine(
            heightlessTarget +
            overshoot +
            Vector3.up * (maxAltitudeDeviation + parentPosition.y - coreHeight),
            heightlessTarget +
            overshoot +
            Vector3.down * maxAltitudeDeviation
            );
    }
}