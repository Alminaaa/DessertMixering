using UnityEngine;

/// <summary>
/// A collectible dessert dropped by a defeated enemy.
/// Flies towards the player when they enter pickup range.
/// Put this on the root object of the loot prefab.
/// </summary>
public class DessertLoot : MonoBehaviour
{
    public enum State
    {
        Grounded,
        Flying,
        Collected
    }

    [Header("Pickup")]
    [Min(0.1f)]
    [SerializeField] private float pickupRange = 0.8f;

    [Min(0.01f)]
    [SerializeField] private float pickupDelay = 0.2f;

    [Header("Fly Animation")]
    [Min(0.01f)]
    [SerializeField] private float flyDuration = 0.3f;

    [Min(0f)]
    [SerializeField] private float flyHeight = 0.25f;

    [SerializeField] private Vector2 targetOffset = new Vector2(0f, 0.2f);

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSFX;

    [SerializeField, Range(0f, 1f)]
    private float pickupSFXVolume = 1f;

    public State CurrentState { get; private set; }

    private Transform player;
    private Vector3 flyStartPosition;
    private Vector3 originalScale;

    private float flyTimer;
    private float pickupTimer;

    private void Awake()
    {
        originalScale = transform.localScale;
        CurrentState = State.Grounded;
    }

    private void Start()
    {
        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();

        if (movement != null)
            player = movement.transform;
    }

    private void Update()
    {
        if (player == null)
            return;

        switch (CurrentState)
        {
            case State.Grounded:
                CheckPickupRange();
                break;

            case State.Flying:
                UpdateFlyAnimation();
                break;
        }
    }

    private void CheckPickupRange()
    {
        pickupTimer += Time.deltaTime;

        if (pickupTimer < pickupDelay)
            return;

        Vector2 direction = (Vector2)player.position - (Vector2)transform.position;

        if (direction.sqrMagnitude <= pickupRange * pickupRange)
            EnterFlying();
    }

    private void EnterFlying()
    {
        CurrentState = State.Flying;
        flyTimer = 0f;
        flyStartPosition = transform.position;
    }

    private void UpdateFlyAnimation()
    {
        flyTimer += Time.deltaTime;

        float t = Mathf.Clamp01(flyTimer / flyDuration);

        // Accelerate towards the player.
        float easedT = t * t;

        Vector3 targetPosition = player.position + (Vector3)targetOffset;
        targetPosition.z = flyStartPosition.z;

        Vector3 position = Vector3.Lerp(flyStartPosition, targetPosition, easedT);

        // Small visual arc during the flight.
        position.y += Mathf.Sin(t * Mathf.PI) * flyHeight;

        transform.position = position;

        // Shrink slightly while approaching the player.
        transform.localScale = originalScale * Mathf.Lerp(1f, 0.25f, t);

        if (t >= 1f)
            EnterCollected();
    }

    private void EnterCollected()
    {
        if (CurrentState == State.Collected)
            return;

        CurrentState = State.Collected;

        SoundPlayer.Play(pickupSFX, pickupSFXVolume, 0.05f);

        // Inventory integration comes in Part 2.

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
#endif
}