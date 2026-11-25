using UnityEngine;

/// <summary>
/// Settings for one enemy kind (Puding, Sutlac...).
/// Create one with: Project window > right click > Create > Game > Enemy Data
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic")]
    public string enemyName = "Puding";

    [Min(1)]
    public int maxHealth = 3;

    [Tooltip("World units per second. The player moves at 5.")]
    [Min(0f)]
    public float baseMoveSpeed = 1.5f;

    public bool canBeDamaged = true;

    [Header("Visual (never changes gameplay values)")]
    [Tooltip("Only scales the sprite. Health, speed and collider stay the same.")]
    [Min(0.1f)]
    public float visualScale = 1f;

    [Tooltip("Color of liquid particles for this dessert.")]
    public Color vfxColor = new Color(0.45f, 0.25f, 0.1f);

    [Tooltip("Size multiplier for hit and death effects.")]
    [Min(0.1f)]
    public float vfxScale = 1f;

    [Header("Idle Movement (seconds)")]
    [Min(0f)] public float idleDurationMin = 3f;
    [Min(0f)] public float idleDurationMax = 6f;
    [Min(0f)] public float moveDurationMin = 3f;
    [Min(0f)] public float moveDurationMax = 6f;

    [Header("Hit")]
    [Min(0f)]
    public float damageFlashDuration = 0.1f;

    public Color damageFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Tooltip("Liquid splash played on every hit. Tinted with VFX Color.")]
    public ParticleSystem hitVFX;

    public AudioClip hitSFX;

    [Range(0f, 1f)]
    public float hitSFXVolume = 1f;

    [Header("Panic")]
    public bool canPanic = true;

    [Tooltip("Panic speed = Base Move Speed x this.")]
    [Min(1f)]
    public float panicSpeedMultiplier = 1.5f;

    [Tooltip("Highest allowed panic speed. Keep it below the player speed (5). 0 = no limit.")]
    [Min(0f)]
    public float panicSpeedCap = 4f;

    [Tooltip("How long the panic lasts after the last hit (seconds).")]
    [Min(0.1f)]
    public float panicDuration = 2f;

    [Tooltip("A new hit restarts the panic timer.")]
    public bool panicResetOnDamage = true;

    [Tooltip("How often a panicking enemy picks a new escape direction (seconds).")]
    [Min(0.1f)]
    public float panicDirectionInterval = 0.8f;

    [Tooltip("Escape directions can differ this much from 'straight away from the player' (degrees).")]
    [Range(0f, 90f)]
    public float panicMaxAngle = 60f;

    public AudioClip panicSFX;

    [Range(0f, 1f)]
    public float panicSFXVolume = 1f;

    [Header("Death")]
    public ParticleSystem deathVFX;

    public AudioClip deathSFX;

    [Range(0f, 1f)]
    public float deathSFXVolume = 1f;

    [Header("Loot")]
    public bool canDropLoot = true;

    public GameObject lootPrefab;

    [Range(0f, 1f)]
    public float lootDropChance = 1f;

    [Min(1)]
    public int lootDropQuantityMin = 1;

    [Min(1)]
    public int lootDropQuantityMax = 1;

    /// <summary>Panic speed, always calculated from the base speed (never stacks).</summary>
    public float PanicMoveSpeed
    {
        get
        {
            float speed = baseMoveSpeed * panicSpeedMultiplier;
            return panicSpeedCap > 0f ? Mathf.Min(speed, panicSpeedCap) : speed;
        }
    }

    private void OnValidate()
    {
        // Keep max values from going below min values.
        if (idleDurationMax < idleDurationMin)
            idleDurationMax = idleDurationMin;

        if (moveDurationMax < moveDurationMin)
            moveDurationMax = moveDurationMin;

        if (lootDropQuantityMax < lootDropQuantityMin)
            lootDropQuantityMax = lootDropQuantityMin;
    }
}