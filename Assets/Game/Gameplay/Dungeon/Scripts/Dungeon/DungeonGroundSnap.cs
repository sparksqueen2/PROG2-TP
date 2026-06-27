using UnityEngine;
using UnityEngine.AI;

public static class DungeonGroundSnap
{
    private const float RaycastHeight = 500f;
    private static readonly RaycastHit[] Hits = new RaycastHit[16];

    public static float GetGroundY(Vector3 worldPosition, float fallbackY = 0f)
    {
        return GetGroundYAt(worldPosition.x, worldPosition.z, fallbackY);
    }

    public static float GetGroundYAt(float x, float z, float fallbackY = 0f)
    {
        var sampled = TrySampleTerrainHeight(x, z);
        if (sampled.HasValue)
            return sampled.Value;

        var origin = new Vector3(x, RaycastHeight, z);
        var count = Physics.RaycastNonAlloc(origin, Vector3.down, Hits, RaycastHeight * 2f, ~0, QueryTriggerInteraction.Ignore);

        var bestY = float.MinValue;
        var found = false;

        for (var i = 0; i < count; i++)
        {
            var hit = Hits[i];
            if (hit.collider == null || ShouldSkipCollider(hit.collider))
                continue;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                found = true;
            }
        }

        return found ? bestY : fallbackY;
    }

    public static bool TrySnapToGround(ref Vector3 position, float fallbackY = 0f)
    {
        position.y = GetGroundYAt(position.x, position.z, fallbackY);
        return true;
    }

    public static bool TrySnapTransformToGround(Transform target)
    {
        if (target == null || ShouldSkipTransform(target))
            return false;

        var bottomY = GetBottomY(target);
        var groundY = GetGroundYAt(target.position.x, target.position.z, bottomY);
        var delta = groundY - bottomY;

        if (Mathf.Abs(delta) < 0.001f)
            return false;

        target.position += new Vector3(0f, delta, 0f);
        return true;
    }

    private static float GetBottomY(Transform target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return target.position.y;

        var bottomY = float.MaxValue;
        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            bottomY = Mathf.Min(bottomY, renderer.bounds.min.y);
        }

        return bottomY == float.MaxValue ? target.position.y : bottomY;
    }

    public static float? TrySampleTerrainHeight(float x, float z)
    {
        return SampleTerrainHeight(x, z);
    }

    private static float? SampleTerrainHeight(float x, float z)
    {
        var worldPosition = new Vector3(x, 0f, z);

        foreach (var terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            var terrainPosition = terrain.transform.position;
            var local = worldPosition - terrainPosition;
            var size = terrain.terrainData.size;

            if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z)
                continue;

            return terrain.SampleHeight(worldPosition) + terrainPosition.y;
        }

        return null;
    }

    public static bool ShouldSkipTransform(Transform target)
    {
        if (target.GetComponentInParent<SectionBlocker>(true) != null)
            return true;

        if (target.GetComponent<Terrain>() != null)
            return true;

        if (target.GetComponent<Light>() != null || target.GetComponent<Camera>() != null)
            return true;

        if (target.GetComponent<NavMeshAgent>() != null)
            return true;

        switch (target.name)
        {
            case "CorruptionVeil":
            case "CorruptionMist":
            case "BarrierVisual":
            case "Label":
            case "NavMeshFloor":
            case "---NAVMESH_WALKABLE---":
                return true;
        }

        return false;
    }

    public static bool IsPlacementRoot(Transform target, Transform snapRoot)
    {
        if (target == null || target == snapRoot || ShouldSkipTransform(target))
            return false;

        if (target.GetComponent<Renderer>() != null)
            return !HasRenderedParent(target, snapRoot);

        if (target.GetComponentInChildren<Renderer>(false) != null)
            return !HasRenderedParent(target, snapRoot);

        return HasMarkerComponent(target);
    }

    private static bool HasRenderedParent(Transform target, Transform snapRoot)
    {
        var parent = target.parent;
        while (parent != null && parent != snapRoot)
        {
            if (parent.GetComponent<Renderer>() != null)
                return true;

            parent = parent.parent;
        }

        return false;
    }

    private static bool HasMarkerComponent(Transform target)
    {
        if (target.GetComponent<PlayerSpawnPoint>() != null)
            return true;

        if (target.GetComponent<DungeonWaveSpawnPoint>() != null)
            return true;

        if (target.GetComponent<DungeonWaveSpawnGroup>() != null)
            return false;

        return target.childCount == 0;
    }

    private static bool ShouldSkipCollider(Collider collider)
    {
        if (collider == null)
            return true;

        if (collider.isTrigger)
            return true;

        if (collider.GetComponentInParent<SectionBlocker>(true) != null)
            return true;

        if (collider.GetComponentInParent<Terrain>() != null)
            return false;

        if (collider.gameObject.name.StartsWith("SectionBlocker"))
            return true;

        return false;
    }
}
