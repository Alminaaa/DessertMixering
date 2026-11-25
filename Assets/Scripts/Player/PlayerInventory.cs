using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player's items and currencies.
/// Items with "takesInventorySpace" share one capacity (raw and cooked desserts).
/// Money and special currencies are unlimited.
///
/// Put this on the root object of the Player.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    public class StartingItem
    {
        public ItemData item;
        [Min(0)] public int amount;
    }

    [Tooltip("How many space-taking items the player can carry. The skill tree will raise it later.")]
    [SerializeField, Min(1)] private int capacity = 20;

    [Tooltip("Given once when the game starts (e.g. starting money). The save system will replace this later.")]
    [SerializeField] private List<StartingItem> startingItems = new List<StartingItem>();

    /// <summary>The player's inventory. There is only one player.</summary>
    public static PlayerInventory Instance { get; private set; }

    public int Capacity => capacity;

    /// <summary>Space used by items the player already holds.</summary>
    public int UsedSpace { get; private set; }

    /// <summary>Space promised to items that are still flying to the player.</summary>
    public int ReservedSpace { get; private set; }

    public int FreeSpace => Mathf.Max(0, capacity - UsedSpace - ReservedSpace);
    public bool IsFull => FreeSpace <= 0;

    public IReadOnlyDictionary<ItemData, int> Items => items;

    /// <summary>Item, new count, change (+ added / - removed).</summary>
    public event Action<ItemData, int, int> ItemChanged;

    /// <summary>Used space or capacity changed.</summary>
    public event Action CapacityChanged;

    /// <summary>Something could not be added because the inventory is full.</summary>
    public event Action FullAttempted;

    private readonly Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[Inventory] There is more than one PlayerInventory in the scene.", this);

        Instance = this;
    }

    private void Start()
    {
        foreach (StartingItem start in startingItems)
        {
            if (start.item != null && start.amount > 0)
                TryAdd(start.item, start.amount);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ---------------------------------------------------------------- Queries

    public int GetCount(ItemData item)
    {
        return item != null && items.TryGetValue(item, out int count) ? count : 0;
    }

    public bool Has(ItemData item, int amount = 1)
    {
        return GetCount(item) >= amount;
    }

    public bool CanAdd(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        return !item.takesInventorySpace || FreeSpace >= amount;
    }

    // ---------------------------------------------------------------- Add / Remove

    /// <summary>Adds the items now. Returns false (and adds nothing) if there is not enough space.</summary>
    public bool TryAdd(ItemData item, int amount)
    {
        if (!CanAdd(item, amount))
        {
            ReportFull(item);
            return false;
        }

        ChangeCount(item, amount);
        return true;
    }

    /// <summary>Removes the items. Returns false (and removes nothing) if the player does not have enough.</summary>
    public bool TryRemove(ItemData item, int amount)
    {
        if (item == null || amount <= 0 || GetCount(item) < amount)
            return false;

        ChangeCount(item, -amount);
        return true;
    }

    // ---------------------------------------------------------------- Reservations (flying loot)

    /// <summary>Keeps space free for an item that is on its way. Call Commit when it arrives.</summary>
    public bool TryReserve(ItemData item, int amount)
    {
        if (!CanAdd(item, amount))
        {
            ReportFull(item);
            return false;
        }

        if (item.takesInventorySpace)
            ReservedSpace += amount;

        return true;
    }

    /// <summary>The reserved item arrived: turn the reservation into a real item.</summary>
    public void CommitReservation(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return;

        if (item.takesInventorySpace)
            ReservedSpace = Mathf.Max(0, ReservedSpace - amount);

        ChangeCount(item, amount);
    }

    /// <summary>The reserved item will never arrive: free the space again.</summary>
    public void CancelReservation(ItemData item, int amount)
    {
        if (item == null || amount <= 0 || !item.takesInventorySpace)
            return;

        ReservedSpace = Mathf.Max(0, ReservedSpace - amount);
    }

    // ---------------------------------------------------------------- Capacity

    public void SetCapacity(int newCapacity)
    {
        capacity = Mathf.Max(1, newCapacity);
        CapacityChanged?.Invoke();
    }

    // ---------------------------------------------------------------- Internal

    private void ChangeCount(ItemData item, int delta)
    {
        int newCount = GetCount(item) + delta;

        if (newCount <= 0)
            items.Remove(item);
        else
            items[item] = newCount;

        if (item.takesInventorySpace)
        {
            UsedSpace += delta;
            CapacityChanged?.Invoke();
        }

        ItemChanged?.Invoke(item, Mathf.Max(0, newCount), delta);

#if UNITY_EDITOR
        string sign = delta > 0 ? "+" : "";
        Debug.Log($"[Inventory] {sign}{delta} {item.displayName} (now {newCount}) | Space {UsedSpace}/{capacity}", this);
#endif
    }

    private void ReportFull(ItemData item)
    {
        if (item != null && item.takesInventorySpace)
            FullAttempted?.Invoke();
    }

#if UNITY_EDITOR
    [ContextMenu("Test / Add 10 Of Each Starting Item")]
    private void TestAddStartingItems()
    {
        foreach (StartingItem start in startingItems)
        {
            if (start.item != null)
                TryAdd(start.item, 10);
        }
    }

    [ContextMenu("Test / Remove 1 Of Every Item")]
    private void TestRemoveOneOfEach()
    {
        foreach (ItemData item in new List<ItemData>(items.Keys))
            TryRemove(item, 1);
    }
#endif
}