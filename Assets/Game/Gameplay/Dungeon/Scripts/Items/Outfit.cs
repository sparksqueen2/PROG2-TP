using UnityEngine;

public enum OutfitSlotPosition 
{ 
    Helmet, 
    Gloves, 
    Boots, 
    Shoulder, 
    Armor 
};

[CreateAssetMenu(fileName = "Outfit", menuName = "Items/Outfit")]
public class Outfit : Item
{
    [Header("Armor Specific")]
    public OutfitSlotPosition type;
    public int defense;

    public override ItemType GetItemType() { return ItemType.Outfit; }

    public override string ItemToString()
    {
        string text = base.ItemToString();
        string thisType = type switch
        {
            OutfitSlotPosition.Armor => "Armadura",
            OutfitSlotPosition.Boots => "Botas",
            OutfitSlotPosition.Gloves => "Guantes",
            OutfitSlotPosition.Helmet => "Casco",
            OutfitSlotPosition.Shoulder => "Hombros",
            _ => "------"
        };

        text += "\nTipo: " + thisType + "\nDefensa: " + defense;

        return text;
    }
}
