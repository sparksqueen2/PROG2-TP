using TMPro;
using UnityEngine;

[ExecuteAlways]
public class SectionBlocker : MonoBehaviour
{
    private const string BarrierVisualName = "BarrierVisual";
    private const string WallSegmentResourcePath = "Dungeon/WallSegment";

    [SerializeField] private int sectionIndex;
    [SerializeField] private string label = "UMBRAL SELLADO";
    [SerializeField] private GameObject wallSegmentPrefab;
    [SerializeField] private float wallSegmentLength = 2f;

    private BoxCollider boxCollider;
    private MeshRenderer meshRenderer;
    private TextMeshPro labelText;
    private bool visualsApplied;

    public int SectionIndex => sectionIndex;

    private void Awake()
    {
        EnsureVisual();
    }

    private void OnEnable()
    {
        EnsureVisual();
        if (!visualsApplied)
            RefreshVisual();
        else
            CorruptionVisual.SetBlockerParticlesActive(gameObject, Application.isPlaying);
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            CorruptionVisual.SetBlockerParticlesActive(gameObject, false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            visualsApplied = false;
            RefreshVisual();
        };
    }
#endif

    private void EnsureVisual()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gameObject.AddComponent<MeshFilter>();

            if (meshFilter.sharedMesh == null)
                meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        if (labelText == null)
        {
            var labelObject = transform.Find("Label");
            if (labelObject == null)
            {
                labelObject = new GameObject("Label").transform;
                labelObject.SetParent(transform, false);
                labelObject.localPosition = new Vector3(0f, 0.55f, 0f);
                labelText = labelObject.gameObject.AddComponent<TextMeshPro>();
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.fontSize = 4f;
            }
            else
            {
                labelText = labelObject.GetComponent<TextMeshPro>();
            }
        }
    }

    public void RefreshVisual()
    {
        EnsureVisual();
        gameObject.isStatic = true;

        if (meshRenderer != null)
            meshRenderer.enabled = false;

        BuildBarrierVisual();
        CorruptionVisual.ApplyToBlocker(gameObject);
        visualsApplied = true;

        if (labelText != null)
        {
            labelText.text = label;
            labelText.color = new Color(0.82f, 0.72f, 0.95f, 1f);
        }

        if (Application.isPlaying)
            CorruptionVisual.SetBlockerParticlesActive(gameObject, true);
    }

    private void BuildBarrierVisual()
    {
        var existing = transform.Find(BarrierVisualName);
        if (existing != null)
            return;

        var prefab = ResolveWallSegmentPrefab();
        if (prefab == null)
            return;

        var dimensions = GetWallDimensions(prefab);
        var parentScale = transform.localScale;
        var runsAlongX = parentScale.x >= parentScale.z;
        var span = runsAlongX ? parentScale.x : parentScale.z;
        var thickness = runsAlongX ? parentScale.z : parentScale.x;
        var count = Mathf.Max(1, Mathf.CeilToInt(span / wallSegmentLength));

        var root = new GameObject(BarrierVisualName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var step = span / count;
        var start = -(span - step) * 0.5f;
        var heightScale = parentScale.y / Mathf.Max(0.01f, dimensions.y);
        var widthScale = step / Mathf.Max(0.01f, dimensions.x);
        var depthScale = thickness / Mathf.Max(0.01f, dimensions.z);

        for (var i = 0; i < count; i++)
        {
            var segment = Instantiate(prefab, root.transform);
            segment.name = $"WallSegment_{i + 1}";

            var offset = start + i * step;
            var localPosition = runsAlongX
                ? new Vector3(offset / parentScale.x, 0f, 0f)
                : new Vector3(0f, 0f, offset / parentScale.z);

            segment.transform.localPosition = localPosition;
            segment.transform.localRotation = runsAlongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
            segment.transform.localScale = new Vector3(widthScale, heightScale, depthScale);
        }
    }

    private static Vector3 GetWallDimensions(GameObject prefab)
    {
        var renderer = prefab.GetComponentInChildren<Renderer>();
        if (renderer == null)
            return new Vector3(2f, 3f, 0.5f);

        return renderer.bounds.size;
    }

    private GameObject ResolveWallSegmentPrefab()
    {
        if (wallSegmentPrefab != null)
            return wallSegmentPrefab;

        wallSegmentPrefab = Resources.Load<GameObject>(WallSegmentResourcePath);

#if UNITY_EDITOR
        if (wallSegmentPrefab == null)
        {
            wallSegmentPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Gameplay/Dungeon/Prefabs/Props/Wall.prefab");
        }
#endif

        return wallSegmentPrefab;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.45f, 0.1f, 0.65f, 0.55f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
