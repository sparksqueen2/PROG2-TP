using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class ForestPropCollider : MonoBehaviour
{
    public void EnsureColliders()
    {
        foreach (var root in GetComponentsInChildren<Transform>(true))
        {
            if (root == transform || DungeonGroundSnap.ShouldSkipTransform(root))
                continue;

            if (!DungeonGroundSnap.IsPlacementRoot(root, transform))
                continue;

            AddColliderToPlacementRoot(root);
        }
    }

    private void Awake()
    {
        EnsureColliders();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            EnsureColliders();
        };
    }
#endif

    private static void AddColliderToPlacementRoot(Transform root)
    {
        if (!ShouldColliderize(root))
            return;

        root.gameObject.isStatic = false;
        ForestPropsLayer.Apply(root.gameObject);
        StripNavMeshSources(root);

        if (root.GetComponent<Collider>() != null)
            return;

        var renderers = root.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var capsule = root.gameObject.AddComponent<CapsuleCollider>();
        capsule.direction = 1;
        capsule.height = Mathf.Max(1.2f, bounds.size.y);
        capsule.radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.32f, 0.25f, 1.4f);
        capsule.center = root.InverseTransformPoint(bounds.center);
    }

    private static bool ShouldColliderize(Transform root)
    {
        var name = root.name.ToLowerInvariant();
        if (name.Contains("grass") || name.Contains("poppy") || name.Contains("mushroom"))
            return false;

        return name.Contains("tree")
               || name.Contains("shrub")
               || name.Contains("rock")
               || name.Contains("stump")
               || name.Contains("logs")
               || name.Contains("menhir")
               || name.Contains("ore");
    }

    private static void StripNavMeshSources(Transform root)
    {
        foreach (var meshCollider in root.GetComponentsInChildren<MeshCollider>(true))
        {
            if (meshCollider == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(meshCollider);
            else
#endif
                Object.Destroy(meshCollider);
        }

#if UNITY_EDITOR
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(
            root.gameObject,
            UnityEditor.StaticEditorFlags.BatchingStatic);
#endif
    }
}
