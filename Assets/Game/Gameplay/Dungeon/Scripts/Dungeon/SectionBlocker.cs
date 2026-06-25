using TMPro;
using UnityEngine;

public class SectionBlocker : MonoBehaviour
{
    [SerializeField] private int sectionIndex;
    [SerializeField] private Color wallColor = new Color(0.1f, 0.03f, 0.14f, 0.78f);
    [SerializeField] private string label = "UMBRAL SELLADO";

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
            CorruptionVisual.SetBlockerParticlesActive(gameObject, true);
    }

    private void OnDisable()
    {
        CorruptionVisual.SetBlockerParticlesActive(gameObject, false);
    }

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
                labelObject.localPosition = new Vector3(0f, 1.2f, 0f);
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
        if (visualsApplied)
            return;

        EnsureVisual();
        gameObject.isStatic = true;
        CorruptionVisual.ApplyToBlocker(gameObject);
        visualsApplied = true;

        if (labelText != null)
        {
            labelText.text = label;
            labelText.color = new Color(0.82f, 0.72f, 0.95f, 1f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.45f, 0.1f, 0.65f, 0.55f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
