using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the enemy's health. Hidden until the first hit, then the bar
/// slides smoothly to the new value.
///
/// Put this on the HealthBarCanvas child of the enemy prefab
/// (the same object that has the Canvas component).
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References (auto-filled if empty)")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private Canvas canvas;

    [Header("References")]
    [SerializeField] private Image healthFill;

    [Header("Behaviour")]
    [SerializeField] private bool hideUntilDamaged = true;

    [Tooltip("How fast the bar moves to the new value (per second). 0 = instantly.")]
    [SerializeField, Min(0f)] private float fillSpeed = 3f;

    private float displayedFill = 1f;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<Enemy>();

        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (healthFill != null)
            healthFill.fillAmount = 1f;

        if (hideUntilDamaged && canvas != null)
            canvas.enabled = false;
    }

    private void LateUpdate()
    {
        if (enemy == null || enemy.Data == null || healthFill == null)
            return;

        // Show the bar the first time the enemy is hit.
        if (hideUntilDamaged && canvas != null && !canvas.enabled && enemy.HasBeenDamaged)
            canvas.enabled = true;

        float target = Mathf.Clamp01((float)enemy.CurrentHealth / enemy.Data.maxHealth);

        displayedFill = fillSpeed <= 0f
            ? target
            : Mathf.MoveTowards(displayedFill, target, fillSpeed * Time.deltaTime);

        healthFill.fillAmount = displayedFill;
    }
}