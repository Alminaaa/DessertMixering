using UnityEngine;

/// <summary>
/// Portrait games: always shows the same world width on every phone.
/// Taller phones simply see a bit more above and below.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraWidthFit : MonoBehaviour
{
    [Tooltip("How many world units should always fit horizontally.")]
    [SerializeField] private float visibleWidth = 10f;

    private Camera cam;
    private float lastAspect = -1f;
    private float lastWidth = -1f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        Apply();
    }

    private void LateUpdate()
    {
        // Only recalculate when something changed.
        if (!Mathf.Approximately(cam.aspect, lastAspect) || !Mathf.Approximately(visibleWidth, lastWidth))
            Apply();
    }

    private void Apply()
    {
        lastAspect = cam.aspect;
        lastWidth = visibleWidth;

        if (lastAspect > 0f)
            cam.orthographicSize = visibleWidth * 0.5f / lastAspect;
    }
}