using UnityEngine;

public class RoomBehaviour : Room
{
    //0-Up, 1-Down, 2-Right, 3-Left
    [SerializeField] private GameObject[] walls = null;
    [SerializeField] private GameObject[] doors = null;

    public override void Init()
    {
        
    }

    public void UpdateRoom(bool[] status)
    {
        if (doors == null || walls == null)
            return;

        var count = Mathf.Min(status.Length, Mathf.Min(doors.Length, walls.Length));
        for (var i = 0; i < count; i++)
        {
            if (doors[i] != null)
                doors[i].SetActive(status[i]);

            if (walls[i] != null)
                walls[i].SetActive(!status[i]);
        }
    }

    public void CloseAllPassages()
    {
        UpdateRoom(new[] { false, false, false, false });
    }

    public void SetPassageOpen(int direction, bool isOpen)
    {
        if (doors == null || walls == null || direction < 0 || direction >= doors.Length)
            return;

        doors[direction].SetActive(isOpen);
        walls[direction].SetActive(!isOpen);
    }
}