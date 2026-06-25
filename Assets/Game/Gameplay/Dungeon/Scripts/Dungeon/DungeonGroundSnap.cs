using UnityEngine;

public static class DungeonGroundSnap
{
    private static readonly RaycastHit[] Hits = new RaycastHit[12];
    private static float? cachedReferenceFloorY;

    public static float GetGroundY(Vector3 worldPosition, float fallbackY = 3f)
    {
        var snapped = worldPosition;
        return TrySnapToGround(ref snapped, fallbackY) ? snapped.y : fallbackY;
    }

    public static bool TrySnapToGround(ref Vector3 position, float fallbackY = 3f)
    {
        var referenceY = GetReferenceFloorY(fallbackY);

        if (TryTerrainHeight(ref position))
        {
            if (Mathf.Abs(position.y - referenceY) > 2.5f)
                position.y = referenceY;
            return true;
        }

        var origin = new Vector3(position.x, position.y + 50f, position.z);
        var count = Physics.RaycastNonAlloc(origin, Vector3.down, Hits, 80f, ~0, QueryTriggerInteraction.Ignore);

        var bestY = float.MinValue;
        var found = false;

        for (var i = 0; i < count; i++)
        {
            var hit = Hits[i];
            if (hit.collider == null || IsExcludedCollider(hit.collider))
                continue;

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                found = true;
            }
        }

        if (!found)
        {
            position.y = referenceY;
            return false;
        }

        if (Mathf.Abs(bestY - referenceY) > 2.5f)
            bestY = referenceY;

        position.y = bestY;
        return true;
    }

    private static float GetReferenceFloorY(float fallbackY)
    {
        if (cachedReferenceFloorY.HasValue)
            return cachedReferenceFloorY.Value;

        var spawn = Object.FindFirstObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Include);
        var anchor = spawn != null ? spawn.transform.position : new Vector3(7f, 0f, -24f);
        var terrain = Terrain.activeTerrain;

        if (terrain != null)
        {
            cachedReferenceFloorY = terrain.SampleHeight(anchor) + terrain.transform.position.y;
            return cachedReferenceFloorY.Value;
        }

        cachedReferenceFloorY = fallbackY;
        return fallbackY;
    }

    private static bool TryTerrainHeight(ref Vector3 position)
    {
        var terrain = Terrain.activeTerrain;
        if (terrain == null)
            return false;

        position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
        return true;
    }

    private static bool IsExcludedCollider(Collider collider)
    {
        if (collider.GetComponentInParent<SectionBlocker>() != null)
            return true;

        return collider.gameObject.name.StartsWith("SectionBlocker");
    }
}
