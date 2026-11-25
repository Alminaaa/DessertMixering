using UnityEngine;

/// <summary>
/// Flips the enemy left / right based on its movement and applies the visual scale.
/// Only this child is scaled, so health, speed and collider never change.
/// Also plays the death effects.
///
/// Put this on the "Visual" child of the enemy prefab.
/// </summary>
public class EnemyVisual : MonoBehaviour
{
    [Header("References (auto-filled if empty)")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private EnemyMotor motor;

    [SerializeField] private bool spriteDrawnFacingRight = true;

    private float facing = 1f;
    private float scale = 1f;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<Enemy>();

        if (motor == null)
            motor = GetComponentInParent<EnemyMotor>();

        if (enemy != null && enemy.Data != null)
            scale = enemy.Data.visualScale;

        Apply();
    }

    private void LateUpdate()
    {
        if (motor == null)
            return;

        // Only flip when clearly moving left or right.
        float vx = motor.Velocity.x;

        if (Mathf.Abs(vx) > 0.05f)
            facing = vx > 0f ? 1f : -1f;

        Apply();
    }

    private void Apply()
    {
        float sign = spriteDrawnFacingRight ? facing : -facing;
        transform.localScale = new Vector3(scale * sign, scale, 1f);
    }

    /// <summary>Plays the death effect and sound using this enemy's data.</summary>
    public void PlayDeathEffects()
    {
        if (enemy == null || enemy.Data == null)
            return;

        EnemyData data = enemy.Data;

        VfxSpawner.Spawn(data.deathVFX, transform.position, data.vfxColor);
        SoundPlayer.Play(data.deathSFX, data.deathSFXVolume, 0.05f);
    }
}