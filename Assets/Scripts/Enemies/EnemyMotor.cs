using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves the enemy with physics, turns away from walls and obstacles,
/// and keeps it inside its EnemyArea.
///
/// Put this on the root object of the enemy prefab.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMotor : MonoBehaviour
{
    [Header("Obstacles")]
    [Tooltip("Layers the enemy treats as walls. Keep only Default (walls, fences, counters).")]
    [SerializeField] private LayerMask obstacleLayers = 1;

    [Tooltip("How far ahead the enemy checks for walls when picking a direction.")]
    [SerializeField] private float lookAhead = 0.75f;

    [Tooltip("How many directions are tried when looking for a free path.")]
    [SerializeField, Range(4, 32)] private int directionSamples = 16;

    [Tooltip("Minimum time between two direction changes. Prevents jitter against walls.")]
    [SerializeField] private float turnCooldown = 0.15f;

    public bool IsMoving { get; private set; }
    public Vector2 Direction { get; private set; }
    public Vector2 Velocity => rb.linearVelocity;

    private Rigidbody2D rb;
    private EnemyArea area;
    private float speed;
    private float turnTimer;
    private float bodyRadius = 0.3f;
    private ContactFilter2D obstacleFilter;
    private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>(4);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        Collider2D body = GetComponent<Collider2D>();
        if (body != null)
            bodyRadius = Mathf.Max(body.bounds.extents.x, body.bounds.extents.y);

        obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(obstacleLayers);
        obstacleFilter.useTriggers = false;
    }

    public void SetArea(EnemyArea newArea)
    {
        area = newArea;
    }

    public void Move(Vector2 direction, float newSpeed)
    {
        Direction = direction.normalized;
        speed = newSpeed;
        IsMoving = true;
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void Stop()
    {
        IsMoving = false;
        Direction = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (turnTimer > 0f)
            turnTimer -= Time.fixedDeltaTime;

        if (!IsMoving)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Turn back before crossing the area border.
        if (area != null)
        {
            Vector2 next = rb.position + Direction * speed * Time.fixedDeltaTime;
            if (!area.Contains(next, bodyRadius))
            {
                Vector2 toCenter = ((Vector2)area.Bounds.center - rb.position).normalized;
                Turn(toCenter, 0.2f, true);
            }
        }

        rb.linearVelocity = Direction * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collision2D collision)
    {
        if (!IsMoving || collision.contactCount == 0)
            return;

        if ((obstacleLayers.value & (1 << collision.gameObject.layer)) == 0)
            return;

        Vector2 away = rb.position - collision.GetContact(0).point;
        if (away.sqrMagnitude < 0.0001f)
            return;

        away.Normalize();

        // Only turn if we are walking into the obstacle, not away from it.
        if (Vector2.Dot(Direction, away) > 0f)
            return;

        Turn(away, 0.1f, false);
    }

    private void Turn(Vector2 preferred, float minDot, bool ignoreCooldown)
    {
        if (!ignoreCooldown && turnTimer > 0f)
            return;

        Vector2 newDirection = FindFreeDirection(preferred, minDot);
        Direction = newDirection != Vector2.zero ? newDirection : preferred.normalized;
        turnTimer = turnCooldown;
    }

    /// <summary>
    /// Returns a random direction that is not blocked by a wall and stays inside the area.
    /// If "preferred" is set, only directions roughly pointing that way are accepted first.
    /// Returns Vector2.zero if every direction is blocked.
    /// </summary>
    public Vector2 FindFreeDirection(Vector2 preferred, float minDot)
    {
        bool usePreference = preferred != Vector2.zero;
        Vector2 preferredNormalized = usePreference ? preferred.normalized : Vector2.zero;

        // First pass with the preference, second pass without it.
        for (int pass = 0; pass < 2; pass++)
        {
            if (pass == 1 && !usePreference)
                break;

            float startAngle = Random.Range(0f, 360f);
            float step = 360f / directionSamples;

            for (int i = 0; i < directionSamples; i++)
            {
                float angle = (startAngle + i * step) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                if (pass == 0 && usePreference && Vector2.Dot(direction, preferredNormalized) < minDot)
                    continue;

                if (IsDirectionFree(direction))
                    return direction;
            }
        }

        return Vector2.zero;
    }

    public bool IsDirectionFree(Vector2 direction)
    {
        if (area != null && !area.Contains(rb.position + direction * lookAhead, bodyRadius))
            return false;

        castHits.Clear();
        int count = rb.Cast(direction, obstacleFilter, castHits, lookAhead);

        for (int i = 0; i < count; i++)
        {
            // A wall we are moving away from does not block us.
            if (Vector2.Dot(castHits[i].normal, direction) < -0.01f)
                return false;
        }

        return true;
    }
}