using UnityEditor.VersionControl;
using UnityEngine;

/// <summary>
/// Fits a panel under the Canvas into the phone's "safe area."
///
/// Usage:
///   Canvas
///    ?? Background      (fullscreen)
///    ?? SafeAreaPanel   (add this script)
///        ?? buttons, score, menu...
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeArea : MonoBehaviour
{
    [Header("On which sides should a safe zone be applied?")]
    [Tooltip("Notch / camera hole / status bar at the top")]
    [SerializeField] private bool applyTop = true;

    [Tooltip("Bottom home screen bar (iPhone) / navigation bar")]
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
        // Recalculate if the screen size, orientation, or safe area changes.
        // (This also ensures that it updates when you change the device in the Device Simulator.)
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

        // Convert the safe area, specified in pixels, into anchor values ??ranging from 0 to 1.
        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;

        anchorMin.x /= width;
        anchorMin.y /= height;
        anchorMax.x /= width;
        anchorMax.y /= height;

        // Push the unwanted edges back against the edge of the screen.
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