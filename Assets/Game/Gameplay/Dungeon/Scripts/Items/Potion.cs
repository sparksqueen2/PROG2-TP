using UnityEngine;

public enum POTION_TYPE
{
    LIFE,
    MANA
}

[CreateAssetMenu(fileName = "Potion", menuName = "Items/Consumable/Potion")]
public class Potion : Consumible
{
    [Header("Potion Specific")]
    public POTION_TYPE type;

    public override string ItemToString()
    {
        string text = base.ItemToString();
        text += "\nTipo de pocion: " + TranslatePotionType(type);
        return text;
    }

    private static string TranslatePotionType(POTION_TYPE potionType)
    {
        return potionType switch
        {
            POTION_TYPE.LIFE => "Vida",
            POTION_TYPE.MANA => "Mana",
            _ => potionType.ToString()
        };
    }
}
