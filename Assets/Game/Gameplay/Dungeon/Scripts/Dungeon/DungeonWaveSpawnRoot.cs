using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class DungeonWaveSpawnRoot : MonoBehaviour
{
    private static readonly (int section, string label, Vector3[] guardians, Vector3[] optionals)[] DefaultLayouts =
    {
        (
            0,
            "Oleada1_Umbral",
            new[]
            {
                new Vector3(-2f, 3f, -25f),
                new Vector3(22f, 3f, -26f)
            },
            new[]
            {
                new Vector3(5f, 3f, -22f),
                new Vector3(13f, 3f, -28f),
                new Vector3(19f, 3f, -24f)
            }
        ),
        (
            1,
            "Oleada2_Camara",
            new[]
            {
                new Vector3(2f, 3f, -8f),
                new Vector3(13f, 3f, -7f)
            },
            new[]
            {
                new Vector3(6f, 3f, -6f),
                new Vector3(10f, 3f, -9f),
                new Vector3(16f, 3f, -7f)
            }
        ),
        (
            2,
            "Oleada3_Grieta",
            new[]
            {
                new Vector3(58f, 3f, -2f),
                new Vector3(66f, 3f, 1f)
            },
            new[]
            {
                new Vector3(62f, 3f, -3f),
                new Vector3(70f, 3f, 0f),
                new Vector3(64f, 3f, 2f)
            }
        )
    };

#if UNITY_EDITOR
    [ContextMenu("Regenerar marcadores de oleada")]
    private void RegenerateMarkersFromMenu()
    {
        EnsureDefaultLayout(forceRebuild: true);
        EditorUtility.SetDirty(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    public void EnsureDefaultLayout(bool forceRebuild = false)
    {
        if (!forceRebuild && transform.childCount > 0)
            return;

        if (forceRebuild)
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        foreach (var layout in DefaultLayouts)
            CreateSectionGroup(layout.section, layout.label, layout.guardians, layout.optionals);
    }

    private void CreateSectionGroup(int sectionIndex, string label, Vector3[] guardians, Vector3[] optionals)
    {
        var groupObject = new GameObject(label);
        groupObject.transform.SetParent(transform, false);

        var group = groupObject.AddComponent<DungeonWaveSpawnGroup>();
        group.Configure(sectionIndex, label);

        for (var i = 0; i < guardians.Length; i++)
            CreateMarker(groupObject.transform, DungeonSpawnRole.Guardian, i, guardians[i]);

        for (var i = 0; i < optionals.Length; i++)
            CreateMarker(groupObject.transform, DungeonSpawnRole.Optional, i, optionals[i]);
    }

    private static void CreateMarker(Transform parent, DungeonSpawnRole role, int slotIndex, Vector3 position)
    {
        var markerObject = new GameObject(role == DungeonSpawnRole.Guardian ? $"Guardian_{slotIndex + 1}" : $"Optional_{slotIndex + 1}");
        markerObject.transform.SetParent(parent, false);
        markerObject.transform.position = position;

        var marker = markerObject.AddComponent<DungeonWaveSpawnPoint>();
        marker.Configure(role, slotIndex);
    }
}
