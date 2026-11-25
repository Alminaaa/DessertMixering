using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-right list with one row per owned item (money, desserts...).
/// Rows appear when the player gets an item and disappear when the count reaches 0.
///
/// Put this on the InventoryHUD object (which has a Vertical Layout Group).
/// </summary>
public class InventoryHUD : MonoBehaviour
{
    [Header("References (auto-filled if empty)")]
    [SerializeField] private PlayerInventory inventory;

    [Tooltip("Where rows are created. Empty = this object.")]
    [SerializeField] private Transform container;

    [Header("References")]
    [SerializeField] private InventoryHudEntry entryPrefab;

    private readonly Dictionary<ItemData, InventoryHudEntry> entries = new Dictionary<ItemData, InventoryHudEntry>();
    private readonly List<ItemData> sortBuffer = new List<ItemData>();

    private void Start()
    {
        if (inventory == null)
            inventory = PlayerInventory.Instance;

        if (inventory == null)
        {
            Debug.LogWarning("[InventoryHUD] No PlayerInventory found.", this);
            return;
        }

        if (container == null)
            container = transform;

        inventory.ItemChanged += OnItemChanged;

        // Show what the player already has (e.g. starting money).
        foreach (KeyValuePair<ItemData, int> pair in inventory.Items)
            OnItemChanged(pair.Key, pair.Value, 0);
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.ItemChanged -= OnItemChanged;
    }

    private void OnItemChanged(ItemData item, int count, int delta)
    {
        if (item == null || !item.showInHud || entryPrefab == null)
            return;

        if (count <= 0)
        {
            if (entries.Remove(item, out InventoryHudEntry oldEntry) && oldEntry != null)
                Destroy(oldEntry.gameObject);

            return;
        }

        if (!entries.TryGetValue(item, out InventoryHudEntry entry))
        {
            entry = Instantiate(entryPrefab, container);
            entries[item] = entry;
            SortEntries();
        }

        entry.Set(item, count, delta != 0);
    }

    /// <summary>Orders the rows by the item's HUD Order (money first).</summary>
    private void SortEntries()
    {
        sortBuffer.Clear();
        sortBuffer.AddRange(entries.Keys);
        sortBuffer.Sort((a, b) =>
        {
            int byOrder = a.hudOrder.CompareTo(b.hudOrder);
            return byOrder != 0 ? byOrder : string.CompareOrdinal(a.displayName, b.displayName);
        });

        for (int i = 0; i < sortBuffer.Count; i++)
            entries[sortBuffer[i]].transform.SetSiblingIndex(i);
    }
}