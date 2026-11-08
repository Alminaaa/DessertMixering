using UnityEngine;

/// <summary>
/// Fits a UI panel inside the phone's "safe area", so buttons and text
/// are never hidden behind the notch, camera hole or home bar.
///
/// Hierarchy:
///   Canvas
///   |- Background      (full screen, NO SafeArea script)
///   |- SafeAreaPanel   (this script, stretched to full screen)
///      |- Buttons, score, menus...
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeArea : MonoBehaviour
{
    [Header("Which edges should respect the safe area?")]
    [Tooltip("Notch / camera hole / status bar")]
    [SerializeField] private bool applyTop = true;

    [Tooltip("Home bar (iPhone) / navigation bar")]
    [SerializeField] private bool applyBottom = true;

    [SerializeField] private bool applyLeft = true;
    [SerializeField] private bool applyRight = true;

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScreenOrientation lastOrientation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        // Recalculate if the screen size, orientation or safe area changes.
        // This also updates when you switch devices in the Device Simulator.
        if (Screen.safeArea != lastSafeArea
            || Screen.width != lastScreenSize.x
            || Screen.height != lastScreenSize.y
            || Screen.orientation != lastOrientation)
        {
            Apply();
        }
    }

    private void Apply()
    {
        Rect safe = Screen.safeArea;
        int width = Screen.width;
        int height = Screen.height;

        lastSafeArea = safe;
        lastScreenSize = new Vector2Int(width, height);
        lastOrientation = Screen.orientation;

        if (width <= 0 || height <= 0)
            return;

        // Convert the safe area from pixels to 0-1 anchor values.
        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;

        anchorMin.x /= width;
        anchorMin.y /= height;
        anchorMax.x /= width;
        anchorMax.y /= height;

        // Edges that should not use the safe area go back to the screen edge.
        if (!applyLeft) anchorMin.x = 0f;
        if (!applyBottom) anchorMin.y = 0f;
        if (!applyRight) anchorMax.x = 1f;
        if (!applyTop) anchorMax.y = 1f;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}