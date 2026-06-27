using UnityEngine;

public class ForestPlayableZoneFilter : MonoBehaviour
{
    public void ApplyPlayableFilter()
    {
        var bounds = PlayableAreaBounds.Instance;
        if (bounds == null)
            bounds = Object.FindFirstObjectByType<PlayableAreaBounds>(FindObjectsInactive.Include);

        foreach (Transform child in transform)
        {
            var position = child.position;
            var inside = bounds == null || bounds.ContainsXZ(position.x, position.z);
            child.gameObject.SetActive(inside);
        }
    }

    private void Start()
    {
        ApplyPlayableFilter();
    }
}
