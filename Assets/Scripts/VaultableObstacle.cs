using UnityEngine;

/// <summary>
/// Marks an obstacle as low enough to jump over (fence, counter).
/// Obstacles WITHOUT this script (walls, stands) simply block the player.
///
/// Needs a Collider2D on the same object. The collider should cover only
/// the base of the obstacle (where it touches the ground), not the whole sprite.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class VaultableObstacle : MonoBehaviour
{
    public enum CrossAxis
    {
        Auto,       // Wide obstacle = crossed up/down, tall obstacle = crossed left/right
        Vertical,   // Always crossed up/down
        Horizontal  // Always crossed left/right
    }

    [Tooltip("Which way the player jumps over this obstacle. Auto works for most fences.")]
    [SerializeField] private CrossAxis crossAxis = CrossAxis.Auto;

    public bool CrossesVertically(Bounds bounds)
    {
        switch (crossAxis)
        {
            case CrossAxis.Vertical: return true;
            case CrossAxis.Horizontal: return false;
            default: return bounds.size.x >= bounds.size.y;
        }
    }

#if UNITY_EDITOR
    // Debug: when selected, a cyan line shows the jump direction in the Scene view.
    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return;

        Bounds b = col.bounds;
        bool vertical = CrossesVertically(b);
        Vector3 axis = vertical ? Vector3.up : Vector3.right;
        float length = (vertical ? b.extents.y : b.extents.x) + 0.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(b.center - axis * length, b.center + axis * length);
    }
#endif
}