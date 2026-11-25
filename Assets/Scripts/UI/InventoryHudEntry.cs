using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row of the inventory HUD: icon + count. Pops a little when the count changes.
/// Without an icon, the item name is written next to the number (handy for placeholders).
///
/// Put this on the root of the HudEntry prefab.
/// </summary>
public class InventoryHudEntry : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countLabel;

    [Header("Pop Animation")]
    [SerializeField] private float popScale = 1.2f;
    [SerializeField, Min(0.01f)] private float popDuration = 0.15f;

    private float popTimer;

    public void Set(ItemData item, int count, bool animate)
    {
        bool hasIcon = item.icon != null;

        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = hasIcon;
        }

        if (countLabel != null)
            countLabel.text = hasIcon ? count.ToString() : $"{item.displayName}: {count}";

        if (animate)
            popTimer = popDuration;
    }

    private void Update()
    {
        if (popTimer <= 0f)
        {
            transform.localScale = Vector3.one;
            return;
        }

        popTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(popTimer / popDuration);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, popScale, t);
    }
}