#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PolytopeForestLayoutBaker
{
    private const string LayoutPrefabPath = "Assets/Game/Gameplay/_Scenes/Prefabs/PolytopeForestLayout.prefab";
    private const string SourceFolder = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";

    private static readonly (string contains, float weight)[] WeightedSources =
    {
        ("Trees/PT_Pine_Tree_03_green.prefab", 12f),
        ("Trees/PT_Fruit_Tree_01_green.prefab", 8f),
        ("Trees/PT_Fruit_Tree_01_apples.prefab", 5f),
        ("Trees/PT_Fruit_Tree_01_pears.prefab", 5f),
        ("Trees/PT_Fruit_Tree_01_plums.prefab", 5f),
        ("Trees/PT_Pine_Tree_03_dead.prefab", 4f),
        ("Trees/PT_Fruit_Tree_01_dead.prefab", 4f),
        ("Shrubs/PT_Generic_Shrub_01_green.prefab", 10f),
        ("Shrubs/PT_Generic_Shrub_01_dead.prefab", 6f),
        ("Plants/PT_Grass_02.prefab", 12f),
        ("Flowers/PT_Poppy_02.prefab", 8f),
        ("Mushrooms/PT_Caesars_Mushroom_01.prefab", 6f),
        ("Rocks/PT_Generic_Rock_01.prefab", 5f),
        ("Rocks/PT_River_Rock_Pile_02.prefab", 4f),
        ("Rocks/PT_Menhir_Rock_02.prefab", 2f),
        ("Rocks/PT_Ore_Rock_01.prefab", 3f),
        ("Trees/PT_Pine_Tree_03_stump.prefab", 4f),
        ("Trees/PT_Fruit_Tree_01_stump.prefab", 4f),
        ("Trees/PT_Pine_Tree_03_logs.prefab", 3f),
        ("Trees/PT_Fruit_Tree_01_logs.prefab", 3f),
    };

    [MenuItem("Game/Tools/Rebuild Polytope Forest Layout")]
    public static void RebuildLayoutPrefab()
    {
        var sourcePrefabs = LoadWeightedPrefabs();
        if (sourcePrefabs.Count == 0)
        {
            Debug.LogError("[Forest] No se encontraron prefabs Polytope.");
            return;
        }

        var bounds = Object.FindFirstObjectByType<PlayableAreaBounds>(FindObjectsInactive.Include);
        if (bounds != null)
            bounds.CacheReferenceGroundY();

        var layoutRoot = new GameObject("---POLYTOPE_FOREST---");
        layoutRoot.AddComponent<ForestPropCollider>();
        layoutRoot.AddComponent<ForestPlayableZoneFilter>();

        var placements = GeneratePlacements(sourcePrefabs, bounds);
        foreach (var placement in placements)
            PlaceInstance(layoutRoot.transform, placement);

        foreach (Transform child in layoutRoot.transform)
            DungeonGroundSnap.TrySnapTransformToGround(child);

        layoutRoot.GetComponent<ForestPropCollider>().EnsureColliders();
        layoutRoot.GetComponent<ForestPlayableZoneFilter>().ApplyPlayableFilter();

        var prefab = PrefabUtility.SaveAsPrefabAsset(layoutRoot, LayoutPrefabPath);
        Object.DestroyImmediate(layoutRoot);

        Debug.Log($"[Forest] Layout regenerado con {placements.Count} props en zona plana.");
        Selection.activeObject = prefab;
    }

    private static List<GameObject> LoadWeightedPrefabs()
    {
        var pool = new List<GameObject>();
        foreach (var (contains, weight) in WeightedSources)
        {
            var path = FindPrefabPath(contains);
            if (string.IsNullOrEmpty(path))
                continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            var count = Mathf.Max(1, Mathf.RoundToInt(weight));
            for (var i = 0; i < count; i++)
                pool.Add(prefab);
        }

        return pool;
    }

    private static string FindPrefabPath(string endsWith)
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { SourceFolder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Replace('\\', '/').EndsWith(endsWith))
                return path;
        }

        return null;
    }

    private static List<(GameObject prefab, Vector3 position, float rotationY, float scale)> GeneratePlacements(
        List<GameObject> pool,
        PlayableAreaBounds bounds)
    {
        const float cellSize = 3.4f;
        const int targetCount = 175;
        const float spawnClearRadius = 5.5f;
        const float corridorClearRadius = 4.5f;
        var spawn = new Vector3(7f, 0f, -27f);
        var corridorEnd = new Vector3(18f, 0f, -10f);

        var minX = bounds != null ? bounds.MinX : PlayableAreaBounds.DefaultMinX;
        var maxX = bounds != null ? bounds.MaxX : PlayableAreaBounds.DefaultMaxX;
        var minZ = bounds != null ? bounds.MinZ : PlayableAreaBounds.DefaultMinZ;
        var maxZ = bounds != null ? bounds.MaxZ : PlayableAreaBounds.DefaultMaxZ;

        var random = new System.Random(20260627);
        var accepted = new List<Vector3>();
        var placements = new List<(GameObject, Vector3, float, float)>();
        var cells = new List<Vector2>();

        for (var x = minX + cellSize * 0.5f; x < maxX; x += cellSize)
        for (var z = minZ + cellSize * 0.5f; z < maxZ; z += cellSize)
            cells.Add(new Vector2(x, z));

        Shuffle(cells, random);

        foreach (var cell in cells)
        {
            if (placements.Count >= targetCount)
                break;

            var jitter = cellSize * 0.35f;
            var position = new Vector3(
                Mathf.Clamp(cell.x + Jitter(random, jitter), minX, maxX),
                0f,
                Mathf.Clamp(cell.y + Jitter(random, jitter), minZ, maxZ));

            if (!IsValidPlacement(position, bounds, spawn, corridorEnd, spawnClearRadius, corridorClearRadius, accepted))
                continue;

            accepted.Add(position);
            placements.Add(CreatePlacement(pool, random, position));
        }

        var attempts = 0;
        while (placements.Count < targetCount && attempts < targetCount * 20)
        {
            attempts++;
            var position = new Vector3(
                minX + (float)random.NextDouble() * (maxX - minX),
                0f,
                minZ + (float)random.NextDouble() * (maxZ - minZ));

            if (!IsValidPlacement(position, bounds, spawn, corridorEnd, spawnClearRadius, corridorClearRadius, accepted))
                continue;

            accepted.Add(position);
            placements.Add(CreatePlacement(pool, random, position));
        }

        return placements;
    }

    private static bool IsValidPlacement(
        Vector3 position,
        PlayableAreaBounds bounds,
        Vector3 spawn,
        Vector3 corridorEnd,
        float spawnClearRadius,
        float corridorClearRadius,
        List<Vector3> accepted)
    {
        if (bounds != null)
        {
            if (!bounds.ContainsXZ(position.x, position.z))
                return false;
        }
        else if (!PlayableAreaBoundsContains(position.x, position.z))
        {
            return false;
        }

        if (Vector3.Distance(position, spawn) < spawnClearRadius)
            return false;

        if (DistanceToSegment(position, spawn, corridorEnd) < corridorClearRadius)
            return false;

        foreach (var other in accepted)
        {
            if (Vector3.Distance(other, position) < 2f)
                return false;
        }

        return true;
    }

    private static (GameObject prefab, Vector3 position, float rotationY, float scale) CreatePlacement(
        List<GameObject> pool,
        System.Random random,
        Vector3 position)
    {
        var prefab = pool[random.Next(pool.Count)];
        var rotationY = (float)random.NextDouble() * 360f;
        var scale = 0.82f + (float)random.NextDouble() * 0.55f;
        return (prefab, position, rotationY, scale);
    }

    private static bool PlayableAreaBoundsContains(float x, float z)
    {
        return x >= PlayableAreaBounds.DefaultMinX
               && x <= PlayableAreaBounds.DefaultMaxX
               && z >= PlayableAreaBounds.DefaultMinZ
               && z <= PlayableAreaBounds.DefaultMaxZ;
    }

    private static void PlaceInstance(Transform parent, (GameObject prefab, Vector3 position, float rotationY, float scale) placement)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(placement.prefab, parent);
        instance.transform.SetPositionAndRotation(
            placement.position,
            Quaternion.Euler(0f, placement.rotationY, 0f));
        instance.transform.localScale = Vector3.one * placement.scale;
    }

    private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        var ab = b - a;
        var t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        var closest = a + ab * t;
        closest.y = point.y;
        return Vector3.Distance(point, closest);
    }

    private static float Jitter(System.Random random, float range)
    {
        return ((float)random.NextDouble() * 2f - 1f) * range;
    }

    private static void Shuffle<T>(List<T> list, System.Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
#endif
