using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public static class DungeonNavMeshSetup
{
    private const string WalkableRootName = "---NAVMESH_WALKABLE---";

    public static void Build()
    {
        PrepareForestProps();

        var root = GetOrCreateWalkableRoot();
        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = root.AddComponent<NavMeshSurface>();

        var bounds = Object.FindFirstObjectByType<PlayableAreaBounds>(FindObjectsInactive.Include);

        surface.collectObjects = CollectObjects.Volume;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~ForestPropsLayer.Mask;
        surface.overrideTileSize = false;
        surface.overrideVoxelSize = false;

        if (bounds != null)
        {
            bounds.CacheReferenceGroundY();
            var centerX = (bounds.MinX + bounds.MaxX) * 0.5f;
            var centerZ = (bounds.MinZ + bounds.MaxZ) * 0.5f;
            surface.center = new Vector3(centerX, bounds.ReferenceGroundY + 6f, centerZ);
            surface.size = new Vector3(
                bounds.MaxX - bounds.MinX + 6f,
                18f,
                bounds.MaxZ - bounds.MinZ + 6f);
        }
        else
        {
            surface.center = new Vector3(21f, 3f, -17f);
            surface.size = new Vector3(64f, 18f, 44f);
        }

        surface.BuildNavMesh();
        WarpAllAgents();

        var spawn = Object.FindFirstObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Include);
        var probe = spawn != null ? spawn.transform.position : new Vector3(7f, 3f, -27f);
        if (!TrySampleWalkable(ref probe))
            Debug.LogWarning("[NavMesh] No se encontró NavMesh cerca del spawn del jugador. Revisá el bake.");
    }

    public static bool TrySampleWalkable(ref Vector3 position, float maxDistance = 40f)
    {
        DungeonGroundSnap.TrySnapToGround(ref position);

        if (!NavMesh.SamplePosition(position, out var hit, maxDistance, NavMesh.AllAreas))
            return false;

        position = hit.position;
        return true;
    }

    public static bool WarpAgent(NavMeshAgent agent)
    {
        if (agent == null)
            return false;

        var position = agent.transform.position;
        if (!TrySampleWalkable(ref position))
            return false;

        agent.Warp(position);

        if (agent.isOnNavMesh && agent.isActiveAndEnabled)
            agent.isStopped = false;

        return agent.isOnNavMesh;
    }

    private static void PrepareForestProps()
    {
        foreach (var filter in Object.FindObjectsByType<ForestPlayableZoneFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            filter.ApplyPlayableFilter();

        foreach (var forest in Object.FindObjectsByType<ForestPropCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            forest.EnsureColliders();
    }

    private static GameObject GetOrCreateWalkableRoot()
    {
        var existing = GameObject.Find(WalkableRootName);
        if (existing != null)
        {
            ConfigureNavMeshPlane(existing.transform);
            return existing;
        }

        var root = new GameObject(WalkableRootName);
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "NavMeshFloor";
        plane.transform.SetParent(root.transform, false);
        plane.isStatic = true;

        var renderer = plane.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;

        ConfigureNavMeshPlane(root.transform);
        return root;
    }

    private static void ConfigureNavMeshPlane(Transform root)
    {
        var plane = root.Find("NavMeshFloor");
        if (plane == null)
            return;

        var bounds = Object.FindFirstObjectByType<PlayableAreaBounds>(FindObjectsInactive.Include);
        if (bounds != null)
        {
            bounds.CacheReferenceGroundY();
            plane.position = new Vector3(
                (bounds.MinX + bounds.MaxX) * 0.5f,
                bounds.ReferenceGroundY,
                (bounds.MinZ + bounds.MaxZ) * 0.5f);
            plane.localScale = new Vector3(
                (bounds.MaxX - bounds.MinX) * 0.1f,
                1f,
                (bounds.MaxZ - bounds.MinZ) * 0.1f);
            return;
        }

        var spawn = Object.FindFirstObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Include);
        var playerPos = spawn != null ? spawn.transform.position : new Vector3(7f, 0f, -27f);
        var center = new Vector3(playerPos.x + 5f, 0f, playerPos.z + 10f);
        var floorY = DungeonGroundSnap.GetGroundY(center);
        plane.position = new Vector3(center.x, floorY, center.z);
        plane.localScale = new Vector3(50f, 1f, 45f);
    }

    private static void WarpAllAgents()
    {
        foreach (var agent in Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            WarpAgent(agent);
    }
}
