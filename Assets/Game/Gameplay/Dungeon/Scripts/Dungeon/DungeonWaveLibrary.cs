using UnityEngine;

[CreateAssetMenu(fileName = "DungeonWaveLibrary", menuName = "Dungeon/Wave Library")]
public class DungeonWaveLibrary : ScriptableObject
{
    [Header("Section 0 - Umbral")]
    public GameObject umbralGuardianA;
    public GameObject umbralGuardianB;
    public GameObject[] umbralOptionalPool;

    [Header("Section 1 - Camara")]
    public GameObject cameraGuardianA;
    public GameObject cameraGuardianB;
    public GameObject[] cameraOptionalPool;

    [Header("Section 2 - Grieta")]
    public GameObject crackGuardianA;
    public GameObject crackGuardianB;
    public GameObject[] crackOptionalPool;
}
