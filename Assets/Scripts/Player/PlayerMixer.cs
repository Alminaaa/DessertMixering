using UnityEngine;

/// <summary>
/// Mixer visuals and sound only: visibility, pointing direction, spinning and the motor sound.
/// All values come from PlayerAttack, so the visuals can never change gameplay.
///
/// Put this on the root object of the Player.
/// </summary>
[RequireComponent(typeof(PlayerAttack))]
public class PlayerMixer : MonoBehaviour
{
    [Header("References (auto-filled if empty)")]
    [SerializeField] private PlayerAttack attack;

    [Header("Mixer Objects")]
    [Tooltip("The mixer root. Best as a child of Visual, so it rises with the jump. Its pivot is the hand.")]
    [SerializeField] private Transform mixer;

    [Tooltip("Optional: the part that spins (placeholder). Leave empty when using an Animator.")]
    [SerializeField] private Transform spinner;

    [Tooltip("Optional: plays the mixer animation. Its speed follows the spin (0 = stopped).")]
    [SerializeField] private Animator animator;

    [Tooltip("All sprites of the mixer, used to draw it in front of or behind the character.")]
    [SerializeField] private SpriteRenderer[] renderers;

    [Header("Holding")]
    [Tooltip("Hand position, relative to the mixer's parent.")]
    [SerializeField] private Vector2 handOffset = new Vector2(0f, -0.1f);

    [Tooltip("How far from the hand the mixer is held.")]
    [SerializeField] private float holdDistance = 0.1f;

    [Tooltip("Flip the sprite when pointing left, so it is never upside down.")]
    [SerializeField] private bool flipWhenPointingLeft = true;

    [Header("Sorting")]
    [SerializeField] private int frontSortingOrder = 1;
    [SerializeField] private int behindSortingOrder = -1;

    [Header("Spin (placeholder)")]
    [Tooltip("Spinner rotation at full speed, degrees per second.")]
    [SerializeField] private float maxSpinSpeed = 1440f;

    [Header("Motor Sound")]
    [Tooltip("A looping AudioSource on a child of the Player (NOT on the Player root, NOT on the mixer).")]
    [SerializeField] private AudioSource motorSource;

    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.8f;

    [Tooltip("Pitch when the mixer is almost stopped. The sound winds down together with the spin.")]
    [SerializeField, Range(0.1f, 1f)] private float minPitch = 0.5f;

    private void Awake()
    {
        if (attack == null)
            attack = GetComponent<PlayerAttack>();

        if (mixer != null)
            mixer.gameObject.SetActive(false);

        if (motorSource != null)
        {
            motorSource.playOnAwake = false;
            motorSource.loop = true;
            motorSource.spatialBlend = 0f;
            motorSource.Stop();
        }
    }

    private void LateUpdate()
    {
        if (attack == null || mixer == null)
            return;

        float spin = attack.MixerSpin01;
        UpdateSound(spin);

        bool visible = attack.MixerVisible;
        if (mixer.gameObject.activeSelf != visible)
            mixer.gameObject.SetActive(visible);

        if (!visible)
            return;

        UpdatePose(attack.AimDirection);
        UpdateSpin(spin);
    }

    private void UpdatePose(Vector2 direction)
    {
        mixer.localPosition = new Vector3(
            handOffset.x + direction.x * holdDistance,
            handOffset.y + direction.y * holdDistance,
            mixer.localPosition.z);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        mixer.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (renderers == null)
            return;

        // Pointing up = the mixer is behind the character's body.
        int order = direction.y > 0.3f ? behindSortingOrder : frontSortingOrder;
        bool flip = flipWhenPointingLeft && direction.x < 0f;

        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer == null)
                continue;

            spriteRenderer.sortingOrder = order;
            spriteRenderer.flipY = flip;
        }
    }

    private void UpdateSpin(float spin)
    {
        if (spinner != null && spin > 0f)
            spinner.Rotate(0f, 0f, -maxSpinSpeed * spin * Time.deltaTime, Space.Self);

        if (animator != null)
            animator.speed = spin;
    }

    private void UpdateSound(float spin)
    {
        if (motorSource == null)
            return;

        if (spin > 0.001f)
        {
            if (!motorSource.isPlaying)
                motorSource.Play();

            motorSource.volume = maxVolume * spin;
            motorSource.pitch = Mathf.Lerp(minPitch, 1f, spin);
        }
        else if (motorSource.isPlaying)
        {
            motorSource.Stop();
        }
    }
}