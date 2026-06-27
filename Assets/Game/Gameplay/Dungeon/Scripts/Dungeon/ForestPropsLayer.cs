using UnityEngine;

public static class ForestPropsLayer
{
    public const string LayerName = "ForestProps";
    public const int Layer = 9;

    public static int Mask => 1 << Layer;

    public static void Apply(GameObject target)
    {
        if (target == null)
            return;

        SetLayerRecursive(target.transform);
    }

    public static void SetLayerRecursive(Transform root)
    {
        root.gameObject.layer = Layer;
        foreach (Transform child in root)
            SetLayerRecursive(child);
    }
}
