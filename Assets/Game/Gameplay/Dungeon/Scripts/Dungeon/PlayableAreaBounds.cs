using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayableAreaBounds : MonoBehaviour
{
    public const float DefaultMinX = -8f;
    public const float DefaultMaxX = 50f;
    public const float DefaultMinZ = -36f;
    public const float DefaultMaxZ = 2f;

    [SerializeField] private float minX = DefaultMinX;
    [SerializeField] private float maxX = DefaultMaxX;
    [SerializeField] private float minZ = DefaultMinZ;
    [SerializeField] private float maxZ = DefaultMaxZ;
    [SerializeField] private float referenceGroundY;

    public static PlayableAreaBounds Instance { get; private set; }

    public float MinX => minX;
    public float MaxX => maxX;
    public float MinZ => minZ;
    public float MaxZ => maxZ;
    public float ReferenceGroundY => referenceGroundY;

    private void Awake()
    {
        Instance = this;
        CacheReferenceGroundY();
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool ContainsXZ(float x, float z)
    {
        return x >= minX && x <= maxX && z >= minZ && z <= maxZ;
    }

    public void CacheReferenceGroundY()
    {
        var spawn = Object.FindFirstObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Include);
        var anchor = spawn != null ? spawn.transform.position : new Vector3(7f, 0f, -27f);
        var terrainHeight = DungeonGroundSnap.TrySampleTerrainHeight(anchor.x, anchor.z);
        referenceGroundY = terrainHeight ?? DungeonGroundSnap.GetGroundYAt(anchor.x, anchor.z, anchor.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (referenceGroundY == 0f)
            CacheReferenceGroundY();

        Gizmos.color = new Color(0.2f, 0.85f, 0.35f, 0.35f);
        var center = new Vector3((minX + maxX) * 0.5f, referenceGroundY + 0.05f, (minZ + maxZ) * 0.5f);
        var size = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(0.95f, 0.75f, 0.1f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
}
