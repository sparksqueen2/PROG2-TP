using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ForestVisualOverhaul
{
    private static readonly string[] DungeonPropKeywords =
    {
        "candle", "candelabra", "lantern", "skull", "skeleton", "bone", "ribcage",
        "deco_", "chain", "shackle", "statue", "cage", "barrel", "pot_", "plinth",
        "WoodPlank", "chest_small", "torch", "shackles"
    };

    private static readonly string[] DungeonMaterialKeywords =
    {
        "Dungeon", "FloorMat", "Medieval", "Deco"
    };

    private static readonly (string resource, string meshGuid)[] TreeSources =
    {
        ("ForestTheme/BirchTree_1", "19152e0aac4e4e2489f4b0cb8a1a3051"),
        ("ForestTheme/BirchTree_2", "d78f4295d26c81446bdda82fe0682a11"),
        ("ForestTheme/BirchTree_3", "cda602052a4afee4bad5f1d258c8bfea"),
        ("ForestTheme/BirchTree_4", "b83cb466f1888cf4487a044fb0b7e3d6"),
        ("ForestTheme/BirchTree_5", "5c6b3c050bea0034c81558d22af10815"),
    };

    private const int TreeCount = 280;
    private const int GrassPatchCount = 90;
    private const float ForestMinX = -30f;
    private const float ForestMaxX = 90f;
    private const float ForestMinZ = -20f;
    private const float ForestMaxZ = 60f;
    private const float SpawnClearRadius = 4f;
    private const float RoomExtent = 15f;
    private const float CameraViewPadding = 40f;

    public static void Apply()
    {
        ApplyAtmosphere();
        ConfigureSunlight();
        PrepareTerrain();
    }

    private static void ApplyAtmosphere()
    {
        var skybox = Resources.Load<Material>("ForestTheme/ForestSkybox");
        if (skybox != null)
            RenderSettings.skybox = skybox;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.fogColor = new Color(0.62f, 0.78f, 0.58f, 1f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.72f, 0.58f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.58f, 0.38f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.38f, 0.22f);
        RenderSettings.ambientIntensity = 1.15f;
        RenderSettings.reflectionIntensity = 0.65f;

        AdjustPostProcessing();
    }

    private static void AdjustPostProcessing()
    {
        var volume = Object.FindFirstObjectByType<Volume>();
        if (volume == null || volume.profile == null)
            return;

        if (volume.profile.TryGet(out Bloom bloom))
        {
            bloom.intensity.value = 0.35f;
            bloom.threshold.value = 1.1f;
        }

        if (volume.profile.TryGet(out Vignette vignette))
            vignette.intensity.value = 0.08f;
    }

    private static void ConfigureSunlight()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional)
                continue;

            light.color = new Color(1f, 0.94f, 0.78f);
            light.intensity = 1.25f;
            light.colorTemperature = 4200f;
            light.useColorTemperature = true;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = light;
            break;
        }
    }

    private static void DisableDungeonLights()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
                continue;

            light.enabled = false;
        }
    }

    private static void HideDungeonProps()
    {
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var obj in allObjects)
        {
            if (!obj.activeInHierarchy)
                continue;

            if (obj.name == "Props")
            {
                obj.SetActive(false);
                continue;
            }

            var lowerName = obj.name.ToLowerInvariant();
            foreach (var keyword in DungeonPropKeywords)
            {
                if (lowerName.Contains(keyword))
                {
                    obj.SetActive(false);
                    break;
                }
            }
        }
    }

    private static void HideDungeonFloorVisuals()
    {
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name != "Floor")
                continue;

            foreach (var renderer in transform.GetComponentsInChildren<MeshRenderer>(true))
                renderer.enabled = false;
        }
    }

    private static void RetextureDungeonSurfaces()
    {
        var forestFloor = Resources.Load<Material>("ForestTheme/ForestFloorMat");
        if (forestFloor == null)
            return;

        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!renderer.enabled || renderer.sharedMaterials == null)
                continue;

            var materials = renderer.sharedMaterials;
            var changed = false;

            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null)
                    continue;

                if (ShouldRetexture(material.name))
                {
                    materials[i] = forestFloor;
                    changed = true;
                }
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }
    }

    private static bool ShouldRetexture(string materialName)
    {
        foreach (var keyword in DungeonMaterialKeywords)
        {
            if (materialName.Contains(keyword))
                return true;
        }

        return false;
    }

    private static void PrepareTerrain()
    {
        var terrain = Object.FindFirstObjectByType<Terrain>();
        if (terrain == null)
            return;

        terrain.gameObject.SetActive(true);
        terrain.drawTreesAndFoliage = true;
    }

    private static void GetPlayAreaBounds(out float minX, out float maxX, out float minZ, out float maxZ)
    {
        minX = ForestMinX;
        maxX = ForestMaxX;
        minZ = ForestMinZ;
        maxZ = ForestMaxZ;

        var rooms = Object.FindObjectsByType<RoomBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (rooms.Length > 0)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            minZ = float.MaxValue;
            maxZ = float.MinValue;

            foreach (var room in rooms)
            {
                var position = room.transform.position;
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minZ = Mathf.Min(minZ, position.z);
                maxZ = Mathf.Max(maxZ, position.z);
            }
        }

        minX -= RoomExtent;
        maxX += RoomExtent;
        minZ -= RoomExtent;
        maxZ += RoomExtent;
    }

    private static void PopulateForest()
    {
        var forestRoot = new GameObject("---FOREST---");
        var treePrefabs = LoadTreePrefabs();
        var grassPrefab = Resources.Load<GameObject>("ForestTheme/GrassPatch");
        var player = Object.FindFirstObjectByType<PlayerController>();
        var clearPosition = player != null ? player.transform.position : Vector3.zero;

        SpawnTrees(forestRoot.transform, treePrefabs, clearPosition);
        SpawnGrassPatches(forestRoot.transform, grassPrefab, clearPosition);
    }

    private static List<GameObject> LoadTreePrefabs()
    {
        var prefabs = new List<GameObject>();

        foreach (var (resource, _) in TreeSources)
        {
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        return prefabs;
    }

    private static void SpawnTrees(Transform parent, List<GameObject> treePrefabs, Vector3 clearPosition)
    {
        if (treePrefabs.Count == 0)
            return;

        var spawned = 0;
        var attempts = 0;
        var maxAttempts = TreeCount * 8;

        while (spawned < TreeCount && attempts < maxAttempts)
        {
            attempts++;
            var position = RandomForestPosition();
            if (Vector3.Distance(position, clearPosition) < SpawnClearRadius)
                continue;

            var prefab = treePrefabs[Random.Range(0, treePrefabs.Count)];
            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var scale = Random.Range(0.75f, 1.35f);
            var tree = Object.Instantiate(prefab, position, rotation, parent);
            tree.transform.localScale = Vector3.one * scale;
            spawned++;
        }
    }

    private static void SpawnGrassPatches(Transform parent, GameObject grassPrefab, Vector3 clearPosition)
    {
        if (grassPrefab == null)
            return;

        for (var i = 0; i < GrassPatchCount; i++)
        {
            var position = RandomForestPosition();
            if (Vector3.Distance(position, clearPosition) < 2.5f)
                continue;

            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var scale = Random.Range(0.6f, 1.4f);
            var grass = Object.Instantiate(grassPrefab, position, rotation, parent);
            grass.transform.localScale = Vector3.one * scale;
        }
    }

    private static Vector3 RandomForestPosition()
    {
        GetPlayAreaBounds(out var minX, out var maxX, out var minZ, out var maxZ);

        return new Vector3(
            Random.Range(minX + CameraViewPadding, maxX - CameraViewPadding),
            0f,
            Random.Range(minZ + CameraViewPadding, maxZ - CameraViewPadding));
    }
}
