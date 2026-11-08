using UnityEngine;

/// <summary>
/// Keeps the target in the center of the screen.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Tooltip("0 = camera is locked exactly on the target. Small values like 0.1 give a soft follow.")]
    [SerializeField] private float smoothTime = 0f;

    [Tooltip("Shift the view, e.g. (0, 1) shows a bit more above the player.")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    private Vector3 velocity;

    public void SetTarget(Transform newTarget, bool snap = true)
    {
        target = newTarget;
        if (snap)
            SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        transform.position = GetDesiredPosition();
        velocity = Vector3.zero;
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        // LateUpdate runs after the player has moved this frame, so there is no jitter.
        if (target == null)
            return;

        Vector3 desired = GetDesiredPosition();

        transform.position = smoothTime <= 0f
            ? desired
            : Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 targetPosition = target.position;

        // Keep the camera's own Z, otherwise it would end up inside the 2D scene.
        return new Vector3(targetPosition.x + offset.x, targetPosition.y + offset.y, transform.position.z);
    }
}