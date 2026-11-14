using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// TEMPORARY debug helper. Add it to the root of Enemy_Base,
/// press Play, wait a few seconds and read the Console. Remove it when the bug is fixed.
/// </summary>
public class EnemyDiagnostics : MonoBehaviour
{
    [SerializeField] private float logInterval = 3f;

    private static readonly Vector2[] TestDirections =
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(1f, 1f).normalized, new Vector2(-1f, 1f).normalized,
        new Vector2(1f, -1f).normalized, new Vector2(-1f, -1f).normalized
    };

    private Enemy enemy;
    private EnemyMotor motor;
    private Rigidbody2D rb;
    private float timer = 1f;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        motor = GetComponent<EnemyMotor>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        timer = logInterval;

        StringBuilder sb = new StringBuilder();
        sb.Append($"[Diag] {name}");
        sb.Append($" | Enemy enabled: {(enemy != null && enemy.enabled)}");
        sb.Append($" | Data: {(enemy != null && enemy.Data != null ? enemy.Data.name : "NONE")}");
        sb.Append($" | Area: {(enemy != null && enemy.Area != null ? enemy.Area.name : "NONE")}");
        sb.Append($" | State: {(enemy != null ? enemy.CurrentState.ToString() : "-")}");
        sb.Append($" | Motor moving: {(motor != null && motor.IsMoving)}");
        sb.Append($" | Velocity: {rb.linearVelocity}");
        sb.Append($" | Body: {rb.bodyType}, Constraints: {rb.constraints}, Simulated: {rb.simulated}");
        sb.Append($" | Layer: {LayerMask.LayerToName(gameObject.layer)}");

        if (motor != null)
        {
            int freeCount = 0;
            StringBuilder blockers = new StringBuilder();

            foreach (Vector2 dir in TestDirections)
            {
                if (motor.IsDirectionFree(dir))
                {
                    freeCount++;
                    continue;
                }

                blockers.Append($"\n   {dir} blocked by: {DescribeBlocker(dir)}");
            }

            sb.Append($"\n   Free directions: {freeCount}/8");
            sb.Append(blockers);
        }

        Debug.Log(sb.ToString(), this);
    }

    private string DescribeBlocker(Vector2 dir)
    {
        if (enemy != null && enemy.Area != null &&
            !enemy.Area.Contains(rb.position + dir * 0.75f, 0.3f))
        {
            return "area border";
        }

        hits.Clear();
        ContactFilter2D everything = new ContactFilter2D();
        everything.NoFilter();
        int count = rb.Cast(dir, everything, hits, 0.75f);

        if (count == 0)
            return "unknown";

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            Collider2D col = hits[i].collider;
            sb.Append($"{col.name} (layer {LayerMask.LayerToName(col.gameObject.layer)}, trigger {col.isTrigger}) ");
        }

        return sb.ToString();
    }
}