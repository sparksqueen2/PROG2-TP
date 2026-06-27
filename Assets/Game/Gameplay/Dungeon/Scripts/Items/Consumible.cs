using UnityEngine;

[CreateAssetMenu(fileName = "Consumable", menuName = "Items/Consumable")]
public class Consumible : Arms
{
    [Header("Consumible Specific")]
    public int amount = 0;

    public override string ItemToString()
    {
        string text = base.ItemToString();
        text += "\nTipo: Consumible\nCantidad: " + amount;
        return text;
    }
}