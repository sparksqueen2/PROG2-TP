using UnityEngine;

public enum WeaponType 
{ 
    Sword, 
    Wand,
    Bow 
}

[CreateAssetMenu(fileName = "Weapon", menuName = "Items/Arms/Weapon")]
public class Weapon : Arms
{
    [Header("Weapon Specific")]
    public WeaponType type;
    public bool twoHanded;
    public int damage;
    public int speed;

    public WeaponType GetWeaponType() => type;

    public override string ItemToString()
    {
        string text = base.ItemToString();
        text += "\nTipo: " + TranslateWeaponType(type) + "\n";
        text += twoHanded ? "(Dos manos)" : "(Una mano)";
        text += "\nDano: " + damage + "\nVelocidad: " + speed;

        return text;
    }

    private static string TranslateWeaponType(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Sword => "Espada",
            WeaponType.Wand => "Varita",
            WeaponType.Bow => "Arco",
            _ => weaponType.ToString()
        };
    }
}