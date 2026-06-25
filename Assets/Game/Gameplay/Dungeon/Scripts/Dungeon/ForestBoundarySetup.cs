using UnityEngine;

public static class ForestBoundarySetup
{
    private static readonly string[] BoundaryContainerNames = { "Walls", "Entrances" };

    public static void RemoveDungeonBoundaries()
    {
        // Mantener paredes/puertas de salas para progresión por secciones.
    }

    private static void DisableBoundaryContainers()
    {
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var transform in allTransforms)
        {
            foreach (var containerName in BoundaryContainerNames)
            {
                if (transform.name == containerName)
                {
                    transform.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    private static void DisableTaggedWalls()
    {
        try
        {
            var walls = GameObject.FindGameObjectsWithTag("Wall");

            foreach (var wall in walls)
            {
                wall.SetActive(false);
            }
        }
        catch (UnityException)
        {
            // Tag "Wall" may not exist in all project configurations.
        }
    }

    private static void DisableStandaloneWalls()
    {
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var obj in allObjects)
        {
            if (!obj.activeInHierarchy)
                continue;

            if (obj.name == "Wall" || obj.name.StartsWith("Wall ("))
            {
                if (obj.GetComponent<MeshCollider>() != null)
                    obj.SetActive(false);
            }
        }
    }

    private static void OpenAllRoomPassages()
    {
        var rooms = Object.FindObjectsByType<RoomBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var openPassages = new[] { true, true, true, true };

        foreach (var room in rooms)
        {
            room.UpdateRoom(openPassages);
        }
    }
}
