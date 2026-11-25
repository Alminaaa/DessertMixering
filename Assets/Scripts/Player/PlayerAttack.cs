using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player attack logic: Exploration / Ready / Attack / Shutdown states,
/// auto-aim at the nearest enemy and damage along the mixer.
///
/// Only gameplay lives here. Mixer visuals and sound are in PlayerMixer,
/// so "mixer is visible" never automatically means "attacking".
///
/// Put this on the root object of the Player.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAttack : MonoBehaviour
{
    public enum State
    {
        Exploration, // Outside enemy areas: no mixer, faster movement, no attacks
        Ready,       // Inside an area, no enemy in range: mixer visible, not spinning
        Attack,      // Enemy in range: mixer spins, damage every attack interval
        Shutdown     // No enemy left: mixer slows down for a moment, no damage, can resume
    }

    [Header("Mixer")]
    [Tooltip("Turn off until the player picks up the mixer (for the tutorial later).")]
    [SerializeField] private bool hasMixer = true;

    [Header("Range")]
    [Tooltip("The attack starts when an enemy is closer than this to the player.")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.4f;

    [Tooltip("Distance from the player to the mixer tip.")]
    [SerializeField, Min(0f)] private float mixerReach = 0.8f;

    [Tooltip("Width of the damage zone along the mixer. Keep reach + radius >= attack range, so every detected enemy can be hit.")]
    [SerializeField, Min(0.05f)] private float hitRadius = 0.6f;

    [Tooltip("How fast the mixer turns toward the target (degrees per second). 0 = instantly.")]
    [SerializeField, Min(0f)] private float aimTurnSpeed = 720f;

    [Header("Damage")]
    [Tooltip("Attacks per second.")]
    [SerializeField, Min(0.01f)] private float attackRate = 3f;

    [Tooltip("Damage dealt to each enemy per attack.")]
    [SerializeField, Min(1)] private int damagePerHit = 1;

    [Header("Shutdown")]
    [Tooltip("How long the mixer takes to slow down and stop.")]
    [SerializeField, Min(0f)] private float shutdownDuration = 1f;

    [Header("Movement Speed")]
    [Tooltip("Speed multiplier outside enemy areas.")]
    [SerializeField, Min(0.1f)] private float explorationSpeedMultiplier = 1.2f;

    [Tooltip("Speed multiplier while attacking. 1 = normal speed.")]
    [SerializeField, Min(0.1f)] private float attackSpeedMultiplier = 1f;

    [Header("Area Border")]
    [Tooltip("Leaving an area only counts after this delay. Stops flickering on the border.")]
    [SerializeField, Min(0f)] private float areaExitDelay = 0.2f;

    public State CurrentState { get; private set; } = State.Exploration;
    public EnemyArea CurrentArea { get; private set; }
    public bool IsInEnemyArea => CurrentArea != null;

    public IReadOnlyList<Enemy> Targets => targets;
    public bool HasTargets => targets.Count > 0;
    public float AttackInterval => 1f / Mathf.Max(0.01f, attackRate);

    /// <summary>Direction the mixer points to (length 1).</summary>
    public Vector2 AimDirection { get; private set; } = Vector2.down;

    /// <summary>World position of the mixer tip (at ground level).</summary>
    public Vector2 MixerTip => (Vector2)transform.position + AimDirection * mixerReach;

    public bool HasMixer => hasMixer;

    /// <summary>True when the mixer should be shown in the hand.</summary>
    public bool MixerVisible => hasMixer && CurrentState != State.Exploration;

    /// <summary>1 = spinning at full speed, 0 = stopped. Drives the mixer animation and sound together.</summary>
    public float MixerSpin01 { get; private set; }

    private PlayerMovement movement;
    private Enemy aimTarget;
    private float attackTimer;
    private float shutdownTimer;
    private float areaExitTimer;

    private readonly List<Enemy> targets = new List<Enemy>();
    private readonly List<Enemy> hitBuffer = new List<Enemy>();

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    /// <summary>Give or take away the mixer (used by the tutorial later).</summary>
    public void SetHasMixer(bool value)
    {
        hasMixer = value;
        if (!hasMixer)
            EnterExploration();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        UpdateCurrentArea(dt);
        UpdateTargets();
        UpdateAim(dt);

        if (attackTimer > 0f)
            attackTimer -= dt;

        switch (CurrentState)
        {
            case State.Exploration:
                if (hasMixer && IsInEnemyArea)
                    EnterReady();
                break;

            case State.Ready:
                if (!IsInEnemyArea)
                    EnterExploration();
                else if (HasTargets)
                    EnterAttack();
                break;

            case State.Attack:
                if (!HasTargets)
                    EnterShutdown();
                else
                    TryAttack();
                break;

            case State.Shutdown:
                shutdownTimer -= dt;

                if (HasTargets)
                    EnterAttack(); // An enemy came back: resume without stopping.
                else if (shutdownTimer <= 0f)
                {
                    if (IsInEnemyArea)
                        EnterReady();
                    else
                        EnterExploration();
                }
                break;
        }

        UpdateSpin();
        UpdateMoveSpeed();
    }

    /// <summary>
    /// Finds the area the player stands in. Checked by position every frame,
    /// so it also works while jumping. Leaving only counts after a short delay.
    /// </summary>
    private void UpdateCurrentArea(float dt)
    {
        EnemyArea inside = null;
        Vector2 position = transform.position;

        foreach (EnemyArea area in EnemyArea.All)
        {
            if (area != null && area.isActiveAndEnabled && area.Contains(position))
            {
                inside = area;
                break;
            }
        }

        if (inside != null)
        {
            CurrentArea = inside;
            areaExitTimer = areaExitDelay;
        }
        else if (CurrentArea != null)
        {
            areaExitTimer -= dt;
            if (areaExitTimer <= 0f)
                CurrentArea = null;
        }
    }

    /// <summary>Collects living enemies of the current area within attack range, and picks the nearest.</summary>
    private void UpdateTargets()
    {
        targets.Clear();
        aimTarget = null;

        if (!hasMixer || CurrentArea == null)
            return;

        Vector2 position = transform.position;
        float rangeSqr = attackRange * attackRange;
        float nearestSqr = float.MaxValue;

        foreach (Enemy enemy in CurrentArea.Enemies)
        {
            if (!IsValid(enemy))
                continue;

            float distanceSqr = ((Vector2)enemy.transform.position - position).sqrMagnitude;
            if (distanceSqr > rangeSqr)
                continue;

            targets.Add(enemy);

            if (distanceSqr < nearestSqr)
            {
                nearestSqr = distanceSqr;
                aimTarget = enemy;
            }
        }
    }

    private void UpdateAim(float dt)
    {
        Vector2 desired;

        if (aimTarget != null)
        {
            desired = (Vector2)aimTarget.transform.position - (Vector2)transform.position;
        }
        else if (CurrentState == State.Shutdown)
        {
            // Keep pointing where we were attacking while slowing down.
            return;
        }
        else
        {
            // No target: hold the mixer in the walking direction.
            desired = movement.FacingDirection;
        }

        if (desired.sqrMagnitude < 0.0001f)
            return;

        desired.Normalize();

        if (aimTurnSpeed <= 0f)
        {
            AimDirection = desired;
            return;
        }

        float current = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
        float target = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;
        float angle = Mathf.MoveTowardsAngle(current, target, aimTurnSpeed * dt) * Mathf.Deg2Rad;

        AimDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    /// <summary>Damages every enemy touching the mixer, once per attack interval.</summary>
    private void TryAttack()
    {
        if (attackTimer > 0f || CurrentArea == null)
            return;

        Vector2 start = transform.position;
        Vector2 tip = MixerTip;
        float radiusSqr = hitRadius * hitRadius;

        // Collect first, damage after: a dying enemy removes itself from the area list.
        hitBuffer.Clear();

        foreach (Enemy enemy in CurrentArea.Enemies)
        {
            if (IsValid(enemy) && DistanceToSegmentSqr(enemy.transform.position, start, tip) <= radiusSqr)
                hitBuffer.Add(enemy);
        }

        // Nothing touched yet (the mixer is still turning): try again next frame.
        if (hitBuffer.Count == 0)
            return;

        foreach (Enemy enemy in hitBuffer)
            enemy.TakeDamage(damagePerHit, transform.position);

        attackTimer = AttackInterval;
    }

    private void UpdateSpin()
    {
        switch (CurrentState)
        {
            case State.Attack:
                MixerSpin01 = 1f;
                break;

            case State.Shutdown:
                MixerSpin01 = shutdownDuration > 0f ? Mathf.Clamp01(shutdownTimer / shutdownDuration) : 0f;
                break;

            default:
                MixerSpin01 = 0f;
                break;
        }
    }

    private void UpdateMoveSpeed()
    {
        if (!IsInEnemyArea)
            movement.SpeedMultiplier = explorationSpeedMultiplier;
        else if (CurrentState == State.Attack)
            movement.SpeedMultiplier = attackSpeedMultiplier;
        else
            movement.SpeedMultiplier = 1f;
    }

    private static bool IsValid(Enemy enemy)
    {
        return enemy != null && enemy.isActiveAndEnabled && !enemy.IsDead;
    }

    private static float DistanceToSegmentSqr(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSqr = ab.sqrMagnitude;

        if (lengthSqr < 0.0001f)
            return (point - a).sqrMagnitude;

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSqr);
        Vector2 closest = a + ab * t;
        return (point - closest).sqrMagnitude;
    }

    private void EnterExploration()
    {
        CurrentState = State.Exploration;
        Log("Exploration");
    }

    private void EnterReady()
    {
        CurrentState = State.Ready;
        Log("Ready");
    }

    private void EnterAttack()
    {
        CurrentState = State.Attack;
        Log($"Attack | Targets: {targets.Count}");
    }

    private void EnterShutdown()
    {
        CurrentState = State.Shutdown;
        shutdownTimer = shutdownDuration;
        Log("Shutdown");
    }

    private void Log(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[PlayerAttack] {message}", this);
#endif
    }

    private void OnDisable()
    {
        targets.Clear();
        hitBuffer.Clear();
        aimTarget = null;
        CurrentArea = null;
        CurrentState = State.Exploration;
        MixerSpin01 = 0f;
        attackTimer = 0f;

        if (movement != null)
            movement.SpeedMultiplier = 1f;
    }

#if UNITY_EDITOR
    // Yellow circle = attack range. Red/green capsule = damage zone along the mixer.
    private void OnDrawGizmosSelected()
    {
        Vector3 position = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, attackRange);

        Vector3 tip = Application.isPlaying
            ? (Vector3)MixerTip
            : position + Vector3.down * mixerReach;

        Gizmos.color = CurrentState == State.Attack ? Color.red : Color.green;
        Gizmos.DrawLine(position, tip);
        Gizmos.DrawWireSphere(position, hitRadius);
        Gizmos.DrawWireSphere(tip, hitRadius);
    }
#endif
}