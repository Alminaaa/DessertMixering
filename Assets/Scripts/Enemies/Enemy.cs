using UnityEngine;

/// <summary>
/// The enemy's brain. Switches between Idle (standing) and Wander (walking).
/// Panic and Death states will be added in the next parts.
///
/// Put this on the root object of the enemy prefab.
/// </summary>
[RequireComponent(typeof(EnemyMotor))]
public class Enemy : MonoBehaviour
{
    public enum State
    {
        Idle,   // Standing still for a random time
        Wander  // Walking in a random free direction for a random time
    }

    [SerializeField] private EnemyData data;

    public EnemyData Data => data;
    public EnemyArea Area { get; private set; }
    public State CurrentState { get; private set; }

    private const float RetryDelay = 0.5f;

    private EnemyMotor motor;
    private float stateTimer;

    private void Awake()
    {
        motor = GetComponent<EnemyMotor>();
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
        stateTimer = Random.Range(data.idleDurationMin, data.idleDurationMax);
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
        stateTimer = Random.Range(data.moveDurationMin, data.moveDurationMax);
    }

    private void OnDestroy()
    {
        if (Area != null)
            Area.Unregister(this);
    }
}