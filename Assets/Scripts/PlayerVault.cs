using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Lets the player jump over low obstacles that have a VaultableObstacle component.
/// The jump starts automatically the moment the player walks into the obstacle head-on.
/// During the jump the player cannot steer and always lands on the other side.
///
/// Put this on the root "Player" object (next to PlayerMovement).
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerVault : MonoBehaviour
{
    [Header("References (auto-filled if empty)")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private SortingGroup sortingGroup;
    [SerializeField] private AudioSource audioSource;

    [Header("Jump")]
    [Tooltip("How long the jump takes in seconds. Make the jump animation the same length.")]
    [SerializeField] private float vaultDuration = 0.4f;

    [Tooltip("How high the character visually rises (world units). Purely visual.")]
    [SerializeField] private float jumpHeight = 0.6f;

    [Tooltip("Extra space between the obstacle and the landing spot.")]
    [SerializeField] private float landingGap = 0.05f;

    [Tooltip("Max angle between the joystick and the jump direction. Bigger = easier to jump when approaching diagonally.")]
    [SerializeField, Range(0f, 80f)] private float maxApproachAngle = 35f;

    [Tooltip("Short wait after landing before another jump can start.")]
    [SerializeField] private float cooldown = 0.1f;

    [Header("Landing Check")]
    [Tooltip("Layers that can block the landing spot. If the spot is blocked, the player does not jump.")]
    [SerializeField] private LayerMask blockingLayers = ~0;

    [Header("Sorting")]
    [Tooltip("While in the air the player is drawn on top of the obstacle.")]
    [SerializeField] private int airSortingBoost = 100;

    [Header("Sound")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;

    public bool IsVaulting { get; private set; }

    /// <summary>Jump direction: up, down, left or right.</summary>
    public Vector2 VaultDirection { get; private set; } = Vector2.down;

    /// <summary>0 at the start of the jump, 1 at the end.</summary>
    public float VisualProgress =>
        IsVaulting ? Mathf.Clamp01((Time.time - vaultStartTime) / vaultDuration) : 0f;

    /// <summary>0 on the ground, 1 at the top of the jump.</summary>
    public float NormalizedHeight
    {
        get
        {
            float t = VisualProgress;
            return IsVaulting ? 4f * t * (1f - t) : 0f;
        }
    }

    /// <summary>How far the sprite should be lifted, in world units.</summary>
    public float HeightOffset => NormalizedHeight * jumpHeight;

    public event Action VaultStarted;
    public event Action VaultEnded;

    private Rigidbody2D rb;
    private ContactFilter2D landingFilter;
    private readonly List<Collider2D> landingHits = new List<Collider2D>(8);

    private bool hasPendingVault;
    private Vector2 pendingEndPosition;
    private Vector2 pendingDirection;

    private Vector2 startPosition;
    private Vector2 endPosition;
    private float elapsed;
    private float vaultStartTime;
    private float cooldownTimer;
    private int baseSortingOrder;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (sortingGroup == null) sortingGroup = GetComponent<SortingGroup>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (sortingGroup != null)
            baseSortingOrder = sortingGroup.sortingOrder;

        landingFilter = new ContactFilter2D();
        landingFilter.SetLayerMask(blockingLayers);
        landingFilter.useTriggers = false;
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (IsVaulting)
        {
            if (elapsed >= vaultDuration)
            {
                EndVault();
            }
            else
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / vaultDuration);
                rb.MovePosition(Vector2.Lerp(startPosition, endPosition, t));
            }
            return;
        }

        // The vault is prepared in the collision callback and started here,
        // because changing the Rigidbody inside a collision callback is unsafe.
        if (hasPendingVault)
            StartVault();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsVaulting || hasPendingVault || cooldownTimer > 0f)
            return;

        if (!collision.collider.TryGetComponent(out VaultableObstacle obstacle) || !obstacle.isActiveAndEnabled)
            return;

        TryPrepareVault(obstacle, collision.collider);
    }

    private void TryPrepareVault(VaultableObstacle obstacle, Collider2D obstacleCollider)
    {
        Vector2 input = movement.MoveInput;
        if (input.sqrMagnitude < 0.01f)
            return;

        Bounds obstacleBounds = obstacleCollider.bounds;
        Bounds playerBounds = bodyCollider.bounds;
        Vector2 playerCenter = playerBounds.center;
        bool crossVertical = obstacle.CrossesVertically(obstacleBounds);

        // The player must stand in front of the obstacle, not at its end.
        if (crossVertical)
        {
            if (playerCenter.x < obstacleBounds.min.x || playerCenter.x > obstacleBounds.max.x)
                return;
        }
        else
        {
            if (playerCenter.y < obstacleBounds.min.y || playerCenter.y > obstacleBounds.max.y)
                return;
        }

        // Jump away from the side the player is on.
        Vector2 direction = crossVertical
            ? (playerCenter.y < obstacleBounds.center.y ? Vector2.up : Vector2.down)
            : (playerCenter.x < obstacleBounds.center.x ? Vector2.right : Vector2.left);

        // The joystick must point at the obstacle, not along it.
        if (Vector2.Angle(input, direction) > maxApproachAngle)
            return;

        // Landing spot: just past the far side of the obstacle.
        Vector2 landingCenter = playerCenter;
        if (crossVertical)
        {
            landingCenter.y = direction.y > 0f
                ? obstacleBounds.max.y + playerBounds.extents.y + landingGap
                : obstacleBounds.min.y - playerBounds.extents.y - landingGap;
        }
        else
        {
            landingCenter.x = direction.x > 0f
                ? obstacleBounds.max.x + playerBounds.extents.x + landingGap
                : obstacleBounds.min.x - playerBounds.extents.x - landingGap;
        }

        // Do not jump if something is in the way at the landing spot.
        if (IsLandingBlocked(landingCenter, playerBounds.size))
            return;

        // The collider center can differ from the object position (e.g. collider at the feet).
        Vector2 colliderOffset = playerCenter - rb.position;

        pendingEndPosition = landingCenter - colliderOffset;
        pendingDirection = direction;
        hasPendingVault = true;
    }

    private bool IsLandingBlocked(Vector2 center, Vector2 size)
    {
        landingHits.Clear();
        int count = Physics2D.OverlapBox(center, size * 0.95f, 0f, landingFilter, landingHits);

        for (int i = 0; i < count; i++)
        {
            // Ignore the player's own colliders.
            if (landingHits[i].attachedRigidbody == rb)
                continue;

            return true;
        }

        return false;
    }

    private void StartVault()
    {
        hasPendingVault = false;
        IsVaulting = true;

        startPosition = rb.position;
        endPosition = pendingEndPosition;
        VaultDirection = pendingDirection;
        elapsed = 0f;
        vaultStartTime = Time.time;

        // Take control away from the joystick and move the body manually.
        movement.ControlLocked = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (sortingGroup != null)
            sortingGroup.sortingOrder = baseSortingOrder + airSortingBoost;

        PlaySound(jumpSound);
        VaultStarted?.Invoke();
    }

    private void EndVault()
    {
        RestoreState();
        cooldownTimer = cooldown;

        PlaySound(landSound);
        VaultEnded?.Invoke();
    }

    private void RestoreState()
    {
        IsVaulting = false;
        hasPendingVault = false;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        movement.ControlLocked = false;

        if (sortingGroup != null)
            sortingGroup.sortingOrder = baseSortingOrder;
    }

    private void OnDisable()
    {
        // If the player is disabled mid-jump, never leave it stuck as Kinematic.
        if (IsVaulting)
            RestoreState();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}