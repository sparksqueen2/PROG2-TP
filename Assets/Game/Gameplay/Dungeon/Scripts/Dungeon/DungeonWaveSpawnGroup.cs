using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonWaveSpawnGroup : MonoBehaviour
{
    [SerializeField] private int sectionIndex;
    [SerializeField] private bool useChildMarkers = true;

    public int SectionIndex => sectionIndex;

    public void Configure(int section, string groupName)
    {
        sectionIndex = section;
        gameObject.name = groupName;
    }

    public Vector3[] GetGuardianSpawns(int count)
    {
        return BuildSpawnArray(count, guardian: true);
    }

    public Vector3[] GetOptionalSpawns(int count)
    {
        return BuildSpawnArray(count, guardian: false);
    }

    private Vector3[] BuildSpawnArray(int count, bool guardian)
    {
        var positions = CollectPositions(guardian);
        if (positions.Count == 0)
            return null;

        var result = new Vector3[count];
        var anchor = positions[0];

        for (var i = 0; i < count; i++)
            result[i] = i < positions.Count ? positions[i] : anchor;

        return result;
    }

    private List<Vector3> CollectPositions(bool guardian)
    {
        var positions = new List<Vector3>();

        if (!useChildMarkers)
            return positions;

        var markers = GetComponentsInChildren<DungeonWaveSpawnPoint>(true)
            .Where(marker => marker.IsGuardian == guardian)
            .OrderBy(marker => marker.SlotIndex);

        foreach (var marker in markers)
            positions.Add(marker.Position);

        return positions;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.55f, 0.2f, 0.85f, 0.35f);
        foreach (var marker in GetComponentsInChildren<DungeonWaveSpawnPoint>(true))
            Gizmos.DrawLine(transform.position, marker.transform.position);
    }
}
