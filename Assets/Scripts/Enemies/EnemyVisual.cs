using UnityEngine;

/// <summary>
/// Enemy visuals: left/right flip, visual scale, hit flash, hit splash,
/// panic look (placeholder) and death effects.
/// Only this child is scaled, so health, speed and collider never change.
///
/// Put this on the "Visual" child of the enemy prefab.
/// </summary>
public class EnemyVisual : MonoBehaviour
{
    [Header("References (auto-filled if empty)")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private EnemyMotor motor;

    [Tooltip("Sprites that flash when hit. Empty = every sprite under this object.")]
    [SerializeField] private SpriteRenderer[] flashRenderers;

    [SerializeField] private bool spriteDrawnFacingRight = true;

    [Header("Panic (placeholder until the real animation)")]
    [Tooltip("Shown only while panicking, e.g. a red '!' above the head.")]
    [SerializeField] private GameObject panicIndicator;

    [Tooltip("Side-to-side wobble while panicking (degrees). 0 = off.")]
    [SerializeField] private float panicWobbleAngle = 12f;

    [SerializeField] private float panicWobbleSpeed = 25f;

    private float facing = 1f;
    private float scale = 1f;
    private float flashTimer;
    private Color[] originalColors;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<Enemy>();

        if (motor == null)
            motor = GetComponentInParent<EnemyMotor>();

        if (enemy != null && enemy.Data != null)
            scale = enemy.Data.visualScale;

        if (flashRenderers == null || flashRenderers.Length == 0)
            flashRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        // Remember the real colors once, so overlapping flashes never lose them.
        originalColors = new Color[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
            originalColors[i] = flashRenderers[i] != null ? flashRenderers[i].color : Color.white;

        if (panicIndicator != null)
            panicIndicator.SetActive(false);

        Apply();
    }

    private void OnEnable()
    {
        if (enemy != null)
            enemy.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (enemy != null)
            enemy.Damaged -= OnDamaged;
    }

    private void LateUpdate()
    {
        if (motor != null)
        {
            // Only flip when clearly moving left or right.
            float vx = motor.Velocity.x;
            if (Mathf.Abs(vx) > 0.05f)
                facing = vx > 0f ? 1f : -1f;
        }

        Apply();
        UpdateFlash();
        UpdatePanicLook();
    }

    private void Apply()
    {
        float sign = spriteDrawnFacingRight ? facing : -facing;
        transform.localScale = new Vector3(scale * sign, scale, 1f);
    }

    // ---------------------------------------------------------------- Hit

    private void OnDamaged(Enemy damagedEnemy)
    {
        EnemyData data = damagedEnemy.Data;
        if (data == null)
            return;

        StartFlash(data);
        VfxSpawner.Spawn(data.hitVFX, transform.position, data.vfxColor, data.vfxScale);
        SoundPlayer.Play(data.hitSFX, data.hitSFXVolume, 0.1f);
    }

    private void StartFlash(EnemyData data)
    {
        if (data.damageFlashDuration <= 0f)
            return;

        flashTimer = data.damageFlashDuration;

        foreach (SpriteRenderer spriteRenderer in flashRenderers)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = data.damageFlashColor;
        }
    }

    private void UpdateFlash()
    {
        if (flashTimer <= 0f)
            return;

        flashTimer -= Time.deltaTime;
        if (flashTimer > 0f)
            return;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
                flashRenderers[i].color = originalColors[i];
        }
    }

    // ---------------------------------------------------------------- Panic

    private void UpdatePanicLook()
    {
        bool panicking = enemy != null && enemy.IsPanicking;

        if (panicIndicator != null && panicIndicator.activeSelf != panicking)
            panicIndicator.SetActive(panicking);

        float angle = panicking && panicWobbleAngle > 0f
            ? Mathf.Sin(Time.time * panicWobbleSpeed) * panicWobbleAngle
            : 0f;

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ---------------------------------------------------------------- Death

    /// <summary>Plays the death effect and sound using this enemy's data.</summary>
    public void PlayDeathEffects()
    {
        if (enemy == null || enemy.Data == null)
            return;

        EnemyData data = enemy.Data;

        VfxSpawner.Spawn(data.deathVFX, transform.position, data.vfxColor, data.vfxScale);
        SoundPlayer.Play(data.deathSFX, data.deathSFXVolume, 0.05f);
    }
}