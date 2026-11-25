using TMPro;
using UnityEngine;

/// <summary>
/// Shows "8/20" above the player for a short time whenever the used space changes.
/// Turns red when the inventory is full.
///
/// Put this on a small World Space canvas that is a child of the Player.
/// </summary>
public class CapacityPopup : MonoBehaviour
{
    [Header("References (auto-filled if empty)")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private CanvasGroup group;

    [Header("References")]
    [SerializeField] private TMP_Text label;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float showDuration = 2f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.3f;

    [Tooltip("When full, the warning is shown at most this often (seconds).")]
    [SerializeField, Min(0f)] private float fullWarningCooldown = 1f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color fullColor = new Color(1f, 0.3f, 0.3f);

    private float timer;
    private float lastFullWarningTime = -999f;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponentInParent<PlayerInventory>();

        if (group == null)
            group = GetComponent<CanvasGroup>();

        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        if (inventory == null)
            return;

        inventory.CapacityChanged += Show;
        inventory.FullAttempted += OnFullAttempted;
    }

    private void OnDisable()
    {
        if (inventory == null)
            return;

        inventory.CapacityChanged -= Show;
        inventory.FullAttempted -= OnFullAttempted;
    }

    private void Show()
    {
        Refresh();
        timer = showDuration;
    }

    private void OnFullAttempted()
    {
        // Standing on loot with a full inventory would fire this every frame.
        if (Time.time - lastFullWarningTime < fullWarningCooldown)
            return;

        lastFullWarningTime = Time.time;
        Show();
    }

    private void Refresh()
    {
        if (label == null || inventory == null)
            return;

        label.text = $"{inventory.UsedSpace}/{inventory.Capacity}";
        label.color = inventory.UsedSpace >= inventory.Capacity ? fullColor : normalColor;
    }

    private void LateUpdate()
    {
        if (timer <= 0f)
        {
            group.alpha = 0f;
            return;
        }

        timer -= Time.deltaTime;

        // Fully visible, then fade out at the end.
        group.alpha = timer > fadeDuration ? 1f : Mathf.Clamp01(timer / fadeDuration);
    }
}