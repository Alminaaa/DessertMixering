
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

    [Header("Visual (never changes gameplay values)")]
    [Tooltip("Only scales the sprite. Health, speed and collider stay the same.")]
    [Min(0.1f)]
    public float visualScale = 1f;

    [Tooltip("Color of liquid particles for this dessert.")]
    public Color vfxColor = new Color(0.45f, 0.25f, 0.1f);

    [Header("Idle Movement (seconds)")]
    [Min(0f)] public float idleDurationMin = 3f;
    [Min(0f)] public float idleDurationMax = 6f;
    [Min(0f)] public float moveDurationMin = 3f;
    [Min(0f)] public float moveDurationMax = 6f;

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
