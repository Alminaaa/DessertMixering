using UnityEngine;

/// <summary>
/// Spawns a particle effect, tints it and destroys it automatically when it is finished.
/// No setup needed.
///
/// Usage: VfxSpawner.Spawn(prefab, position, color, scale);
/// </summary>
public static class VfxSpawner
{
    public static ParticleSystem Spawn(ParticleSystem prefab, Vector3 position, Color color, float scale = 1f)
    {
        if (prefab == null)
            return null;

        ParticleSystem effect = Object.Instantiate(prefab, position, Quaternion.identity);
        effect.transform.localScale = prefab.transform.localScale * scale;

        // Stop first, so no particle is emitted with the wrong color.
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float lifetime = 0f;

        foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;

            float total = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
            lifetime = Mathf.Max(lifetime, total);
        }

        effect.Play(true);

        // Remove the object when the effect is over, so effects never pile up in the scene.
        Object.Destroy(effect.gameObject, lifetime + 0.1f);

        return effect;
    }
}