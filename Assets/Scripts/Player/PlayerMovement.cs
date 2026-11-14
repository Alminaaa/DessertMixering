using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the player in any direction (360 degrees) with the on-screen joystick.
/// In the editor you can also use WASD / arrow keys for quick testing.
///
/// Put this on the root "Player" object together with a Rigidbody2D and a Collider2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private VirtualJoystick joystick;

    [Tooltip("WASD / arrow keys. Only compiled into the editor and PC builds, never into phone builds.")]
    [SerializeField] private bool keyboardFallback = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("ON: a small joystick push makes the player walk slowly. OFF: always full speed.")]
    [SerializeField] private bool analogSpeed = true;

    [Tooltip("How fast the player reaches full speed (units per second squared). High = snappy, low = slippery.")]
    [SerializeField] private float acceleration = 60f;

    [Tooltip("How fast the player stops when the joystick is released.")]
    [SerializeField] private float deceleration = 80f;

    /// <summary>When true, input is still read but does not move the player (used while jumping).</summary>
    public bool ControlLocked { get; set; }

    /// <summary>Raw input this frame, length 0 to 1.</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>How hard the joystick is pushed, 0 to 1.</summary>
    public float InputStrength => analogSpeed ? MoveInput.magnitude : (IsMoving ? 1f : 0f);

    /// <summary>True while the player is giving movement input.</summary>
    public bool IsMoving => MoveInput.sqrMagnitude > MinInputSqr;

    /// <summary>Last direction the player moved in. Kept while standing still.</summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    /// <summary>Current speed from 0 (standing) to 1 (full speed).</summary>
    public float NormalizedSpeed =>
        moveSpeed > 0f ? Mathf.Clamp01(rb.linearVelocity.magnitude / moveSpeed) : 0f;

    private const float MinInputSqr = 0.0001f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // No gravity, the body never rotates, interpolation removes jitter.
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        // Read input every frame so no touch is missed.
        MoveInput = ReadInput();

        if (IsMoving)
            FacingDirection = MoveInput.normalized;
    }

    private void FixedUpdate()
    {
        if (ControlLocked)
            return;

        // Apply physics at the fixed physics rate.
        Vector2 targetVelocity = analogSpeed
            ? MoveInput * moveSpeed
            : MoveInput.normalized * moveSpeed;

        float rate = targetVelocity.sqrMagnitude > MinInputSqr ? acceleration : deceleration;

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);
    }

    private Vector2 ReadInput()
    {
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (keyboardFallback && input.sqrMagnitude < MinInputSqr && Keyboard.current != null)
        {
            Keyboard kb = Keyboard.current;
            float x = 0f;
            float y = 0f;

            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;

            // Normalize so diagonal movement is not faster.
            input = new Vector2(x, y);
            if (input.sqrMagnitude > 1f)
                input.Normalize();
        }
#endif

        return Vector2.ClampMagnitude(input, 1f);
    }
}