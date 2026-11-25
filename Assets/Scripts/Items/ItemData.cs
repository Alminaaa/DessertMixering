using UnityEngine;

/// <summary>
/// One kind of item or currency: money, raw puding, cooked puding, level shard...
/// Create one with: Project window > right click > Create > Game > Item Data
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Tooltip("Unique id for the save system. Lowercase, no spaces. Never change it after release.")]
    public string id;

    public string displayName = "Puding";

    public Sprite icon;

    [Tooltip("ON: uses inventory capacity (desserts). OFF: unlimited (money, special currencies).")]
    public bool takesInventorySpace = true;

    [Tooltip("Show in the top-right inventory HUD while the player has at least one.")]
    public bool showInHud = true;

    [Tooltip("Lower numbers are shown higher in the HUD. Money = 0.")]
    public int hudOrder = 100;

    private void OnValidate()
    {
        // Fill the id automatically from the file name if it is empty.
        if (string.IsNullOrWhiteSpace(id))
            id = name.ToLowerInvariant().Replace(" ", "_");
    }
}