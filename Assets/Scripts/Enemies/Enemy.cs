
using UnityEngine;

/// <summary>
/// The enemy's brain. Switches between Idle, Wander and Death.
/// Put this on the root object of the enemy prefab.
/// </summary>
[RequireComponent(typeof(EnemyMotor))]
public class Enemy : MonoBehaviour
{
    public enum State
    {
        Idle,
        Wander,
        Death
    }

    [SerializeField] private EnemyData data;

    public EnemyData Data => data;
    public EnemyArea Area { get; private set; }
    public State CurrentState { get; private set; }

    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentState == State.Death;

    private const float RetryDelay = 0.5f;

    private EnemyMotor motor;
    private EnemyVisual visual;
    private float stateTimer;

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
            Debug.LogError(
                $"[Enemy] {name} has no EnemyData assigned.",
                this
            );

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
                Debug.LogWarning(
                    $"[Enemy] {name} is not inside an EnemyArea and can walk anywhere.",
                    this
                );
        }

        // Random first wait.
        EnterIdle();
    }

    private void Update()
    {
        if (IsDead)
            return;

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

    private void EnterIdle()
    {
        CurrentState = State.Idle;

        motor.Stop();

        stateTimer = Random.Range(
            data.idleDurationMin,
            data.idleDurationMax
        );
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

        stateTimer = Random.Range(
            data.moveDurationMin,
            data.moveDurationMax
        );
    }

    /// <summary>
    /// Reduces health and triggers Death when health reaches zero.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (IsDead || data == null || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(
            0,
            CurrentHealth - damage
        );

        if (CurrentHealth == 0)
            EnterDeath();
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

    /// <summary>
    /// Spawns collectible loot based on this enemy's data.
    /// </summary>
    private void DropLoot()
    {
        if (!data.canDropLoot || data.lootPrefab == null)
            return;

        if (data.lootDropChance <= 0f)
            return;

        if (data.lootDropChance < 1f &&
            Random.value >= data.lootDropChance)
            return;

        int quantity = Random.Range(
            data.lootDropQuantityMin,
            data.lootDropQuantityMax + 1
        );

        for (int i = 0; i < quantity; i++)
        {
            Instantiate(
                data.lootPrefab,
                transform.position,
                Quaternion.identity
            );
        }
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

        Debug.Log(
            $"[Enemy] {name} - Health: {CurrentHealth}/{data.maxHealth}",
            this
        );
    }

#endif
}
