using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A rectangle that contains enemies. Spawns its enemies at start
/// and keeps them inside its borders. The player can walk in and out freely.
///
/// Set the size with the BoxCollider2D "Size" field, NOT with the Transform scale.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class EnemyArea : MonoBehaviour
{
    [Serializable]
    public class SpawnEntry
    {
        public Enemy prefab;
        [Min(0)] public int count = 1;
    }

    [Header("Spawning")]
    [SerializeField] private List<SpawnEntry> spawnList = new List<SpawnEntry>();

    [Tooltip("Enemies are not spawned closer than this to walls or obstacles.")]
    [SerializeField] private float spawnClearance = 0.5f;

    [Tooltip("Minimum distance between two spawned enemies.")]
    [SerializeField] private float minSpacing = 1f;

    [Tooltip("Layers that count as obstacles when choosing spawn points. Keep only Default.")]
    [SerializeField] private LayerMask obstacleLayers = 1;

    [SerializeField] private int maxAttemptsPerEnemy = 30;

    /// <summary>Every active enemy area in the scene.</summary>
    public static IReadOnlyList<EnemyArea> All => all;

    public Bounds Bounds => bounds;
    public IReadOnlyList<Enemy> Enemies => enemies;

    private static readonly List<EnemyArea> all = new List<EnemyArea>();

    private BoxCollider2D areaCollider;
    private Bounds bounds;
    private ContactFilter2D obstacleFilter;
    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<Collider2D> overlapHits = new List<Collider2D>(8);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
    }

    private void Awake()
    {
        areaCollider = GetComponent<BoxCollider2D>();
        areaCollider.isTrigger = true;
        bounds = areaCollider.bounds;

        obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(obstacleLayers);
        obstacleFilter.useTriggers = false;
    }

    private void OnEnable()
    {
        if (!all.Contains(this))
            all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    private void Start()
    {
        SpawnAll();
    }

    /// <summary>True if the point is inside the area, at least "margin" away from the border.</summary>
    public bool Contains(Vector2 point, float margin = 0f)
    {
        return point.x >= bounds.min.x + margin
            && point.x <= bounds.max.x - margin
            && point.y >= bounds.min.y + margin
            && point.y <= bounds.max.y - margin;
    }

    public void Register(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void Unregister(Enemy enemy)
    {
        enemies.Remove(enemy);
    }

    private void SpawnAll()
    {
        foreach (SpawnEntry entry in spawnList)
        {
            if (entry.prefab == null)
                continue;

            for (int i = 0; i < entry.count; i++)
            {
                if (TryFindSpawnPoint(out Vector2 point))
                {
                    // Not parented to the area, so the area's scale can never affect enemies.
                    Enemy enemy = Instantiate(entry.prefab, point, Quaternion.identity);
                    enemy.Initialize(this);
                }
                else
                {
                    Debug.LogWarning($"[EnemyArea] {name}: no free spot for {entry.prefab.name}. " +
                                     "Make the area bigger or spawn fewer enemies.", this);
                }
            }
        }
    }

    private bool TryFindSpawnPoint(out Vector2 point)
    {
        for (int attempt = 0; attempt < maxAttemptsPerEnemy; attempt++)
        {
            point = new Vector2(
                UnityEngine.Random.Range(bounds.min.x + spawnClearance, bounds.max.x - spawnClearance),
                UnityEngine.Random.Range(bounds.min.y + spawnClearance, bounds.max.y - spawnClearance));

            if (IsBlocked(point))
                continue;

            if (IsTooCloseToOtherEnemy(point))
                continue;

            return true;
        }

        point = Vector2.zero;
        return false;
    }

    private bool IsBlocked(Vector2 point)
    {
        overlapHits.Clear();
        return Physics2D.OverlapCircle(point, spawnClearance, obstacleFilter, overlapHits) > 0;
    }

    private bool IsTooCloseToOtherEnemy(Vector2 point)
    {
        float minSqr = minSpacing * minSpacing;

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && ((Vector2)enemy.transform.position - point).sqrMagnitude < minSqr)
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    // Draws the area in the Scene view.
    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;

        Bounds b = box.bounds;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}