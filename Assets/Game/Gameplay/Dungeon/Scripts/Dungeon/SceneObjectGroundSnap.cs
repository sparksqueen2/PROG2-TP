using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class SceneObjectGroundSnap : MonoBehaviour
{
    [SerializeField] private bool snapOnAwake = true;
    [SerializeField] private bool includeInactive;

    private void Awake()
    {
        if (snapOnAwake)
            SnapChildren();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            SnapChildren();
        };
    }
#endif

    public void SnapChildren()
    {
        foreach (var child in GetComponentsInChildren<Transform>(includeInactive))
        {
            if (child == transform)
                continue;

            if (!DungeonGroundSnap.IsPlacementRoot(child, transform))
                continue;

            DungeonGroundSnap.TrySnapTransformToGround(child);
        }
    }
}
