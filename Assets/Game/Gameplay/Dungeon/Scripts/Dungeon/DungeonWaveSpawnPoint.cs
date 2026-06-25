using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DungeonWaveSpawnPoint : MonoBehaviour
{
    [SerializeField] private DungeonSpawnRole role = DungeonSpawnRole.Guardian;
    [SerializeField] private int slotIndex;

    public bool IsGuardian => role == DungeonSpawnRole.Guardian;
    public int SlotIndex => slotIndex;
    public Vector3 Position => transform.position;

    public void Configure(DungeonSpawnRole spawnRole, int index)
    {
        role = spawnRole;
        slotIndex = index;
        name = spawnRole == DungeonSpawnRole.Guardian ? $"Guardian_{index + 1}" : $"Optional_{index + 1}";
    }

    private void OnDrawGizmos()
    {
        var color = role == DungeonSpawnRole.Guardian
            ? new Color(1f, 0.78f, 0.12f, 0.95f)
            : new Color(0.65f, 0.68f, 0.75f, 0.85f);

        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, 0.55f);
        Gizmos.color = new Color(color.r, color.g, color.b, 0.25f);
        Gizmos.DrawWireSphere(transform.position, 1.1f);

#if UNITY_EDITOR
        var label = role == DungeonSpawnRole.Guardian ? $"G{slotIndex + 1}" : $"O{slotIndex + 1}";
        Handles.Label(transform.position + Vector3.up * 1.2f, label);
#endif
    }
}
