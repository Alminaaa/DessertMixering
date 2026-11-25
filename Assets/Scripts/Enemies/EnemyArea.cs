using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A rectangle that contains enemies. Spawns its enemies at start,
/// keeps them inside its borders and respawns the ones that die.
/// The player can walk in and out freely.
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

    private struct PendingRespawn
    {
        public SpawnEntry entry;
        public float time;
    }

    [Header("Spawning")]
    [Tooltip("Enemies of this area. The count is also the maximum alive at the same time.")]
    [SerializeField] private List<SpawnEntry> spawnList = new List<SpawnEntry>();

    [Tooltip("Enemies are not spawned closer than this to walls or obstacles.")]
    [SerializeField] private float spawnClearance = 0.5f;

    [Tooltip("Minimum distance between two spawned enemies.")]
    [SerializeField] private float minSpacing = 1f;

    [Tooltip("Layers that count as obstacles when choosing spawn points. Keep only Default.")]
    [SerializeField] private LayerMask obstacleLayers = 1;

    [SerializeField] private int maxAttemptsPerEnemy = 30;

    [Header("Respawn")]
    [SerializeField] private bool respawnEnabled = true;

    [Tooltip("Seconds between a death and the new enemy appearing (random between min and max).")]
    [SerializeField, Min(0f)] private float respawnDelayMin = 5f;
    [SerializeField, Min(0f)] private float respawnDelayMax = 8f;

    [Tooltip("Respawned enemies never appear closer than this to the player.")]
    [SerializeField, Min(0f)] private float minDistanceFromPlayer = 3f;

    [Tooltip("Optional effect when an enemy respawns.")]
    [SerializeField] private ParticleSystem respawnVFX;
    [SerializeField] private Color respawnVFXColor = Color.white;

    /// <summary>Every active enemy area in the scene.</summary>
    public static IReadOnlyList<EnemyArea> All => all;

    public Bounds Bounds => bounds;
    public IReadOnlyList<Enemy> Enemies => enemies;

    private const float RetryDelay = 1f;

    private static readonly List<EnemyArea> all = new List<EnemyArea>();

    private BoxCollider2D areaCollider;
    private Bounds bounds;
    private ContactFilter2D obstacleFilter;
    private Transform player;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<Collider2D> overlapHits = new List<Collider2D>(8);
    private readonly Dictionary<Enemy, SpawnEntry> spawnSources = new Dictionary<Enemy, SpawnEntry>();
    private readonly List<PendingRespawn> pendingRespawns = new List<PendingRespawn>();

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
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
            player = playerMovement.transform;

        SpawnAll();
    }

    private void Update()
    {
        UpdateRespawns();
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

    /// <summary>Called when an enemy dies or is destroyed. Schedules a replacement once.</summary>
    public void Unregister(Enemy enemy)
    {
        enemies.Remove(enemy);

        // Remove from the dictionary first, so a second call (death + destroy) never respawns twice.
        if (!spawnSources.TryGetValue(enemy, out SpawnEntry entry))
            return;

        spawnSources.Remove(enemy);

        if (!respawnEnabled || !isActiveAndEnabled)
            return;

        pendingRespawns.Add(new PendingRespawn
        {
            entry = entry,
            time = Time.time + UnityEngine.Random.Range(respawnDelayMin, respawnDelayMax)
        });
    }

    // ---------------------------------------------------------------- Spawning

    private void SpawnAll()
    {
        foreach (SpawnEntry entry in spawnList)
        {
            if (entry.prefab == null)
                continue;

            for (int i = 0; i < entry.count; i++)
            {
                if (!TrySpawn(entry, false))
                {
                    Debug.LogWarning($"[EnemyArea] {name}: no free spot for {entry.prefab.name}. " +
                                     "Make the area bigger or spawn fewer enemies.", this);
                }
            }
        }
    }

    private void UpdateRespawns()
    {
        for (int i = pendingRespawns.Count - 1; i >= 0; i--)
        {
            PendingRespawn pending = pendingRespawns[i];
            if (Time.time < pending.time)
                continue;

            if (TrySpawn(pending.entry, true))
            {
                pendingRespawns.RemoveAt(i);
            }
            else
            {
                // No good spot right now (e.g. the player is standing in the middle): try again soon.
                pending.time = Time.time + RetryDelay;
                pendingRespawns[i] = pending;
            }
        }
    }

    private bool TrySpawn(SpawnEntry entry, bool isRespawn)
    {
        if (entry.prefab == null)
            return false;

        if (!TryFindSpawnPoint(isRespawn, out Vector2 point))
            return false;

        // Not parented to the area, so the area's scale can never affect enemies.
        Enemy enemy = Instantiate(entry.prefab, point, Quaternion.identity);
        spawnSources[enemy] = entry;
        enemy.Initialize(this);

        if (isRespawn)
            VfxSpawner.Spawn(respawnVFX, point, respawnVFXColor);

        return true;
    }

    private bool TryFindSpawnPoint(bool keepAwayFromPlayer, out Vector2 point)
    {
        float playerDistanceSqr = minDistanceFromPlayer * minDistanceFromPlayer;

        for (int attempt = 0; attempt < maxAttemptsPerEnemy; attempt++)
        {
            point = new Vector2(
                UnityEngine.Random.Range(bounds.min.x + spawnClearance, bounds.max.x - spawnClearance),
                UnityEngine.Random.Range(bounds.min.y + spawnClearance, bounds.max.y - spawnClearance));

            if (keepAwayFromPlayer && player != null &&
                (point - (Vector2)player.position).sqrMagnitude < playerDistanceSqr)
                continue;

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

    private void OnValidate()
    {
        if (respawnDelayMax < respawnDelayMin)
            respawnDelayMax = respawnDelayMin;
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