using UnityEngine;

public class DungeonRequiredEnemy : MonoBehaviour
{
    [SerializeField] private int sectionIndex = 0;

    public int SectionIndex => sectionIndex;

    public void Configure(int index)
    {
        sectionIndex = index;
    }
}
