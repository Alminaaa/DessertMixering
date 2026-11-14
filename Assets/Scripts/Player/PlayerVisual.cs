using UnityEngine;

/// <summary>
/// 4-direction character visuals (up, down, left, right) and the fake jump height.
/// The player moves in 360 degrees, but the sprite always shows the closest of the 4 directions.
///
/// Put this on the "Visual" child of the Player (SpriteRenderer + Animator).
///
/// Animator parameters (names must match exactly):
///   IsMoving  (Bool)
///   IsJumping (Bool)
///   MoveX     (Float)
///   MoveY     (Float)
///   AnimSpeed (Float)  -> used as the Walk state's speed multiplier
/// </summary>
public class PlayerVisual : MonoBehaviour
{
    public enum Direction4 { Down, Up, Left, Right }

    [Header("References (auto-filled if empty)")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerVault vault;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Direction")]
    [SerializeField] private Direction4 startDirection = Direction4.Down;

    [Tooltip("Stops flickering between two directions when moving diagonally. 0 = off, 0.2 is a good value.")]
    [SerializeField, Range(0f, 0.5f)] private float directionStickiness = 0.2f;

    [Tooltip("ON: there is no separate Left animation, the Right animation is mirrored instead.")]
    [SerializeField] private bool mirrorRightForLeft = false;

    [Header("Walk Animation Speed")]
    [Tooltip("Lowest walk animation speed when the joystick is only slightly pushed.")]
    [SerializeField, Range(0.1f, 1f)] private float minWalkAnimSpeed = 0.4f;

    [Header("Jump Visuals")]
    [Tooltip("Optional shadow. Must be a child of Player, NOT of Visual, so it stays on the ground.")]
    [SerializeField] private Transform shadow;

    [Tooltip("Shadow size at the top of the jump (1 = no change).")]
    [SerializeField, Range(0.1f, 1f)] private float shadowMinScale = 0.6f;

    public Direction4 CurrentDirection { get; private set; }

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int AnimSpeedHash = Animator.StringToHash("AnimSpeed");

    private bool hasIsMoving;
    private bool hasIsJumping;
    private bool hasMoveX;
    private bool hasMoveY;
    private bool hasAnimSpeed;

    private Vector3 baseLocalPosition;
    private Vector3 shadowBaseScale;

    private void Awake()
    {
        if (movement == null) movement = GetComponentInParent<PlayerMovement>();
        if (vault == null) vault = GetComponentInParent<PlayerVault>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        baseLocalPosition = transform.localPosition;
        if (shadow != null)
            shadowBaseScale = shadow.localScale;

        CurrentDirection = startDirection;
        CacheAnimatorParameters();
    }

    private void LateUpdate()
    {
        if (movement == null)
            return;

        bool isJumping = vault != null && vault.IsVaulting;
        bool isMoving = !isJumping && movement.IsMoving;

        // While jumping, face the jump direction. Otherwise face the joystick direction.
        // When standing still, keep the last direction.
        if (isJumping)
            CurrentDirection = VectorToDirection(vault.VaultDirection);
        else if (isMoving)
            CurrentDirection = CalculateDirection(movement.MoveInput, CurrentDirection);

        if (spriteRenderer != null)
            spriteRenderer.flipX = mirrorRightForLeft && CurrentDirection == Direction4.Left;

        ApplyJumpHeight();
        UpdateAnimator(isMoving, isJumping);
    }

    private void ApplyJumpHeight()
    {
        float height = vault != null ? vault.HeightOffset : 0f;
        transform.localPosition = baseLocalPosition + new Vector3(0f, height, 0f);

        if (shadow != null)
        {
            float t = vault != null ? vault.NormalizedHeight : 0f;
            shadow.localScale = shadowBaseScale * Mathf.Lerp(1f, shadowMinScale, t);
        }
    }

    private Direction4 CalculateDirection(Vector2 input, Direction4 current)
    {
        float absX = Mathf.Abs(input.x);
        float absY = Mathf.Abs(input.y);
        float bias = 1f + directionStickiness;

        // Near 45 degrees, prefer the axis we are already on.
        bool currentIsHorizontal = current == Direction4.Left || current == Direction4.Right;
        bool horizontal = currentIsHorizontal ? absX * bias >= absY : absX > absY * bias;

        if (horizontal)
            return input.x >= 0f ? Direction4.Right : Direction4.Left;

        return input.y >= 0f ? Direction4.Up : Direction4.Down;
    }

    private Direction4 VectorToDirection(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x >= 0f ? Direction4.Right : Direction4.Left;

        return v.y >= 0f ? Direction4.Up : Direction4.Down;
    }

    private Vector2 DirectionToVector(Direction4 direction, bool applyMirror)
    {
        switch (direction)
        {
            case Direction4.Up: return Vector2.up;
            case Direction4.Right: return Vector2.right;
            case Direction4.Left: return applyMirror ? Vector2.right : Vector2.left;
            default: return Vector2.down;
        }
    }

    private void UpdateAnimator(bool isMoving, bool isJumping)
    {
        if (animator == null)
            return;

        Vector2 animDirection = DirectionToVector(CurrentDirection, mirrorRightForLeft);

        if (hasIsMoving) animator.SetBool(IsMovingHash, isMoving);
        if (hasIsJumping) animator.SetBool(IsJumpingHash, isJumping);
        if (hasMoveX) animator.SetFloat(MoveXHash, animDirection.x);
        if (hasMoveY) animator.SetFloat(MoveYHash, animDirection.y);

        if (hasAnimSpeed)
        {
            float speed = isMoving ? Mathf.Max(minWalkAnimSpeed, movement.InputStrength) : 1f;
            animator.SetFloat(AnimSpeedHash, speed);
        }
    }

    private void CacheAnimatorParameters()
    {
        // Checking once avoids "parameter does not exist" warnings every frame.
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == IsMovingHash) hasIsMoving = true;
            else if (parameter.nameHash == IsJumpingHash) hasIsJumping = true;
            else if (parameter.nameHash == MoveXHash) hasMoveX = true;
            else if (parameter.nameHash == MoveYHash) hasMoveY = true;
            else if (parameter.nameHash == AnimSpeedHash) hasAnimSpeed = true;
        }
    }

#if UNITY_EDITOR
    // Debug: a yellow line shows which way the character faces.
    // Visible in the Scene view, and in the Game view if the Gizmos button is on.
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || movement == null)
            return;

        Vector3 start = movement.transform.position;
        Vector3 end = start + (Vector3)DirectionToVector(CurrentDirection, false);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);
    }
#endif
}