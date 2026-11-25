
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current health of an enemy.
/// Put this on the HealthBarCanvas child of the enemy prefab.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private Image healthFill;

    private int lastHealth = -1;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<Enemy>();
    }

    private void LateUpdate()
    {
        if (enemy == null || enemy.Data == null || healthFill == null)
            return;

        // Only update the UI when health changes.
        if (lastHealth == enemy.CurrentHealth)
            return;

        lastHealth = enemy.CurrentHealth;

        healthFill.fillAmount = Mathf.Clamp01(
            (float)enemy.CurrentHealth / enemy.Data.maxHealth
        );
    }
}
