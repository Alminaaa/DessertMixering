
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the player's attack states, detects enemies
/// and automatically deals damage inside the mixer range.
/// Put this on the root object of the Player.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    public enum State
    {
        Exploration,
        Ready,
        Attack,
        Shutdown
    }

    [Header("Attack")]
    [SerializeField] private Transform attackPoint;

    [Min(0.01f)]
    [SerializeField] private float attackRange = 0.45f;

    [SerializeField] private LayerMask enemyLayers;

    [Header("Damage")]
    [Tooltip("Number of attacks per second.")]
    [Min(0.01f)]
    [SerializeField] private float attackRate = 3f;

    [Tooltip("Damage dealt to each enemy per attack.")]
    [Min(1)]
    [SerializeField] private int damagePerHit = 1;

    public State CurrentState { get; private set; }

    public EnemyArea CurrentArea { get; private set; }

    public bool IsInEnemyArea => CurrentArea != null;

    public IReadOnlyList<Enemy> Targets => targets;

    public bool HasTargets => targets.Count > 0;

    public float AttackInterval =>
        1f / Mathf.Max(0.01f, attackRate);

    private float attackTimer;

    private readonly List<EnemyArea> nearbyAreas =
        new List<EnemyArea>();

    private readonly List<Enemy> targets =
        new List<Enemy>();

    private readonly List<Collider2D> overlapHits =
        new List<Collider2D>(8);

    private ContactFilter2D enemyFilter;

    private void Awake()
    {
        CurrentState = State.Exploration;

        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyLayers);
        enemyFilter.useTriggers = true;
    }

    private void Update()
    {
        UpdateCurrentArea();

        // Never attack outside an enemy area.
        if (!IsInEnemyArea)
        {
            targets.Clear();

            if (CurrentState != State.Exploration)
                EnterExploration();

            return;
        }

        if (CurrentState == State.Exploration)
            EnterReady();
    }

    private void LateUpdate()
    {
        if (!IsInEnemyArea)
            return;

        UpdateTargets();

        // Reduce the cooldown without resetting it
        // when enemies enter or leave the attack range.
        attackTimer = Mathf.Max(
            0f,
            attackTimer - Time.deltaTime
        );

        switch (CurrentState)
        {
            case State.Ready:

                if (HasTargets)
                {
                    EnterAttack();
                    UpdateAttack();
                }

                break;

            case State.Attack:

                if (!HasTargets)
                {
                    EnterReady();
                    break;
                }

                UpdateAttack();

                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyArea area = other.GetComponent<EnemyArea>();

        if (area == null)
            return;

        if (!nearbyAreas.Contains(area))
            nearbyAreas.Add(area);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        EnemyArea area = other.GetComponent<EnemyArea>();

        if (area == null)
            return;

        nearbyAreas.Remove(area);
    }

    /// <summary>
    /// Checks whether the player's center is inside an enemy area.
    /// </summary>
    private void UpdateCurrentArea()
    {
        CurrentArea = null;

        foreach (EnemyArea area in nearbyAreas)
        {
            if (area == null || !area.isActiveAndEnabled)
                continue;

            if (area.Contains(transform.position))
            {
                CurrentArea = area;
                return;
            }
        }
    }

    /// <summary>
    /// Finds all valid enemies inside the mixer's attack range.
    /// </summary>
    private void UpdateTargets()
    {
        targets.Clear();

        if (attackPoint == null ||
            !attackPoint.gameObject.activeInHierarchy ||
            CurrentArea == null)
            return;

        overlapHits.Clear();

        Physics2D.OverlapCircle(
            attackPoint.position,
            attackRange,
            enemyFilter,
            overlapHits
        );

        foreach (Collider2D hit in overlapHits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy == null ||
                !enemy.isActiveAndEnabled ||
                enemy.IsDead)
                continue;

            // Only attack enemies belonging to the current area.
            if (enemy.Area != CurrentArea)
                continue;

            // An enemy may have more than one collider.
            if (!targets.Contains(enemy))
                targets.Add(enemy);
        }
    }

    /// <summary>
    /// Performs an attack when the cooldown has finished.
    /// </summary>
    private void UpdateAttack()
    {
        if (!IsInEnemyArea ||
            !HasTargets ||
            attackTimer > 0f)
            return;

        PerformAttack();

        attackTimer = AttackInterval;

        // Remove enemies killed during this attack.
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] == null || targets[i].IsDead)
                targets.RemoveAt(i);
        }

        if (!HasTargets)
            EnterReady();
    }

    /// <summary>
    /// Deals damage to every valid enemy inside the attack range.
    /// </summary>
    private void PerformAttack()
    {
        foreach (Enemy enemy in targets)
        {
            if (enemy == null ||
                !enemy.isActiveAndEnabled ||
                enemy.IsDead)
                continue;

            enemy.TakeDamage(damagePerHit);
        }
    }

    private void EnterExploration()
    {
        CurrentState = State.Exploration;

#if UNITY_EDITOR
        Debug.Log("[PlayerAttack] Exploration", this);
#endif
    }

    private void EnterReady()
    {
        CurrentState = State.Ready;

#if UNITY_EDITOR
        Debug.Log("[PlayerAttack] Ready", this);
#endif
    }

    private void EnterAttack()
    {
        CurrentState = State.Attack;

#if UNITY_EDITOR
        Debug.Log(
            $"[PlayerAttack] Attack | Targets: {targets.Count}",
            this
        );
#endif
    }

    private void OnDisable()
    {
        nearbyAreas.Clear();
        targets.Clear();
        overlapHits.Clear();

        CurrentArea = null;
        CurrentState = State.Exploration;

        attackTimer = 0f;
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = HasTargets
            ? Color.red
            : Color.green;

        Gizmos.DrawWireSphere(
            attackPoint.position,
            attackRange
        );

        foreach (Enemy enemy in targets)
        {
            if (enemy == null)
                continue;

            Gizmos.DrawLine(
                attackPoint.position,
                enemy.transform.position
            );
        }
    }

#endif
}
