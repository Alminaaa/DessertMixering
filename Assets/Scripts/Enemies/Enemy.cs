using System;
using UnityEngine;

/// <summary>
/// The enemy's brain: Idle, Wander, Panic and Death.
/// Put this on the root object of the enemy prefab.
/// </summary>
[RequireComponent(typeof(EnemyMotor))]
public class Enemy : MonoBehaviour
{
    public enum State
    {
        Idle,   // Standing still for a random time
        Wander, // Walking in a random free direction for a random time
        Panic,  // Hit by the player: running away, faster, no waiting
        Death
    }

    [SerializeField] private EnemyData data;

    public EnemyData Data => data;
    public EnemyArea Area { get; private set; }
    public State CurrentState { get; private set; }

    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentState == State.Death;
    public bool IsPanicking => CurrentState == State.Panic;
    public bool HasBeenDamaged => data != null && CurrentHealth < data.maxHealth;

    /// <summary>Fired every time the enemy takes damage (after health is reduced).</summary>
    public event Action<Enemy> Damaged;

    /// <summary>Fired when the enemy starts to panic (not when the panic is only refreshed).</summary>
    public event Action<Enemy> PanicStarted;

    private const float RetryDelay = 0.5f;

    private EnemyMotor motor;
    private EnemyVisual visual;

    private float stateTimer;
    private float panicTimer;
    private float panicTurnTimer;
    private Vector2 threatPosition;

    private void Awake()
    {
        motor = GetComponent<EnemyMotor>();
        visual = GetComponentInChildren<EnemyVisual>();

        if (data != null)
            CurrentHealth = data.maxHealth;
    }

    /// <summary>Called by EnemyArea right after spawning.</summary>
    public void Initialize(EnemyArea area)
    {
        Area = area;
        area.Register(this);
        motor.SetArea(area);
    }

    private void Start()
    {
        if (data == null)
        {
            Debug.LogError($"[Enemy] {name} has no EnemyData assigned.", this);
            enabled = false;
            return;
        }

        if (IsDead)
            return;

        // Enemies placed by hand inside an area also work.
        if (Area == null)
        {
            EnemyArea parentArea = GetComponentInParent<EnemyArea>();

            if (parentArea != null)
                Initialize(parentArea);
            else
                Debug.LogWarning($"[Enemy] {name} is not inside an EnemyArea and can walk anywhere.", this);
        }

        // Random first wait, so all enemies do not start moving at the same moment.
        EnterIdle();
    }

    private void Update()
    {
        if (IsDead)
            return;

        if (CurrentState == State.Panic)
        {
            UpdatePanic(Time.deltaTime);
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f)
            return;

        switch (CurrentState)
        {
            case State.Idle:
                EnterWander();
                break;

            case State.Wander:
                EnterIdle();
                break;
        }
    }

    // ---------------------------------------------------------------- Idle / Wander

    private void EnterIdle()
    {
        CurrentState = State.Idle;
        motor.Stop();
        stateTimer = UnityEngine.Random.Range(data.idleDurationMin, data.idleDurationMax);
    }

    private void EnterWander()
    {
        Vector2 direction = motor.FindFreeDirection(Vector2.zero, 0f);

        // Boxed in on all sides: wait a little and try again.
        if (direction == Vector2.zero)
        {
            stateTimer = RetryDelay;
            return;
        }

        CurrentState = State.Wander;
        motor.Move(direction, data.baseMoveSpeed);
        stateTimer = UnityEngine.Random.Range(data.moveDurationMin, data.moveDurationMax);
    }

    // ---------------------------------------------------------------- Panic

    private void EnterPanic()
    {
        CurrentState = State.Panic;
        panicTimer = data.panicDuration;
        PickPanicDirection();

        SoundPlayer.Play(data.panicSFX, data.panicSFXVolume, 0.1f);
        PanicStarted?.Invoke(this);
    }

    private void UpdatePanic(float dt)
    {
        panicTimer -= dt;
        if (panicTimer <= 0f)
        {
            EnterIdle();
            return;
        }

        panicTurnTimer -= dt;
        if (panicTurnTimer <= 0f || !motor.IsMoving)
            PickPanicDirection();
    }

    /// <summary>Runs in a random free direction that points away from the player.</summary>
    private void PickPanicDirection()
    {
        Vector2 away = (Vector2)transform.position - threatPosition;
        if (away.sqrMagnitude < 0.0001f)
            away = UnityEngine.Random.insideUnitCircle;

        float minDot = Mathf.Cos(data.panicMaxAngle * Mathf.Deg2Rad);

        // FindFreeDirection falls back to any free direction if every escape route is blocked.
        Vector2 direction = motor.FindFreeDirection(away, minDot);
        if (direction == Vector2.zero)
            direction = away.normalized;

        motor.Move(direction, data.PanicMoveSpeed);

        // Small random variation, so panicking enemies do not turn in sync.
        panicTurnTimer = data.panicDirectionInterval * UnityEngine.Random.Range(0.75f, 1.25f);
    }

    // ---------------------------------------------------------------- Damage / Death

    /// <summary>Damage without a known attacker (used by the test menu).</summary>
    public void TakeDamage(int damage)
    {
        Vector2 randomSource = (Vector2)transform.position + UnityEngine.Random.insideUnitCircle.normalized;
        TakeDamage(damage, randomSource);
    }

    /// <summary>Reduces health, starts or refreshes the panic, and triggers Death at zero health.</summary>
    public void TakeDamage(int damage, Vector2 sourcePosition)
    {
        if (IsDead || data == null || damage <= 0 || !data.canBeDamaged)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        Damaged?.Invoke(this);

        if (CurrentHealth == 0)
        {
            EnterDeath();
            return;
        }

        threatPosition = sourcePosition;

        if (!data.canPanic)
            return;

        if (!IsPanicking)
            EnterPanic();
        else if (data.panicResetOnDamage)
            panicTimer = data.panicDuration;
    }

    private void EnterDeath()
    {
        if (IsDead)
            return;

        CurrentState = State.Death;
        motor.Stop();

        if (Area != null)
            Area.Unregister(this);

        if (visual != null)
            visual.PlayDeathEffects();

        DropLoot();

        Destroy(gameObject);
    }

    /// <summary>Spawns collectible loot based on this enemy's data.</summary>
    private void DropLoot()
    {
        if (!data.canDropLoot || data.lootPrefab == null)
            return;

        if (data.lootDropChance <= 0f)
            return;

        if (data.lootDropChance < 1f && UnityEngine.Random.value >= data.lootDropChance)
            return;

        int quantity = UnityEngine.Random.Range(data.lootDropQuantityMin, data.lootDropQuantityMax + 1);

        for (int i = 0; i < quantity; i++)
            Instantiate(data.lootPrefab, transform.position, Quaternion.identity);
    }

    private void OnDestroy()
    {
        if (Area != null)
            Area.Unregister(this);
    }

#if UNITY_EDITOR
    [ContextMenu("Test / Take 1 Damage")]
    private void TestTakeDamage()
    {
        if (!Application.isPlaying || data == null)
            return;

        TakeDamage(1);
        Debug.Log($"[Enemy] {name} - Health: {CurrentHealth}/{data.maxHealth}", this);
    }
#endif
}