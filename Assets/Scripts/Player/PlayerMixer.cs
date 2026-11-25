
using UnityEngine;

/// <summary>
/// Controls the mixer's visibility, position and facing direction.
/// Put this on the root object of the Player.
/// </summary>
[RequireComponent(typeof(PlayerAttack))]
public class PlayerMixer : MonoBehaviour
{
    [Header("References (auto-filled if empty)")]
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private PlayerVisual playerVisual;

    [Header("Mixer")]
    [SerializeField] private Transform mixer;

    [Header("Position Offsets")]
    [SerializeField] private Vector2 downOffset =
        new Vector2(0f, -0.4f);

    [SerializeField] private Vector2 upOffset =
        new Vector2(0f, 0.4f);

    [SerializeField] private Vector2 leftOffset =
        new Vector2(-0.4f, 0f);

    [SerializeField] private Vector2 rightOffset =
        new Vector2(0.4f, 0f);

    private void Awake()
    {
        if (attack == null)
            attack = GetComponent<PlayerAttack>();

        if (playerVisual == null)
            playerVisual = GetComponentInChildren<PlayerVisual>();

        if (mixer != null)
            mixer.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (attack == null ||
            playerVisual == null ||
            mixer == null)
            return;

        UpdateVisibility();

        if (mixer.gameObject.activeSelf)
            UpdateDirection();
    }

    private void UpdateVisibility()
    {
        bool shouldBeVisible =
            attack.CurrentState != PlayerAttack.State.Exploration;

        // Only change visibility when needed.
        if (mixer.gameObject.activeSelf != shouldBeVisible)
            mixer.gameObject.SetActive(shouldBeVisible);
    }

    private void UpdateDirection()
    {
        Vector2 offset;
        float rotation;

        switch (playerVisual.CurrentDirection)
        {
            case PlayerVisual.Direction4.Up:

                offset = upOffset;
                rotation = 90f;
                break;

            case PlayerVisual.Direction4.Left:

                offset = leftOffset;
                rotation = 180f;
                break;

            case PlayerVisual.Direction4.Right:

                offset = rightOffset;
                rotation = 0f;
                break;

            default:

                offset = downOffset;
                rotation = -90f;
                break;
        }

        mixer.localPosition = new Vector3(
            offset.x,
            offset.y,
            mixer.localPosition.z
        );

        mixer.localRotation = Quaternion.Euler(
            0f,
            0f,
            rotation
        );
    }
}
