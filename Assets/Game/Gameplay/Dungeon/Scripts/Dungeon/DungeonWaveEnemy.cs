using UnityEngine;

public class DungeonWaveEnemy : MonoBehaviour
{
    [SerializeField] private bool isGuardian;
    [SerializeField] private int waveSection;

    public bool IsGuardian => isGuardian;
    public int WaveSection => waveSection;

    public void Configure(bool guardian, int section)
    {
        isGuardian = guardian;
        waveSection = section;
    }
}
