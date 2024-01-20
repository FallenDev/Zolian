namespace Darkages.Types;

public static class ItemSlots
{
    public const int None = 0;
    public const int Weapon = 1;
    public const int Armor = 2;
    public const int Shield = 3;
    public const int Helmet = 4;
    public const int Earring = 5;
    public const int Necklace = 6;
    public const int LHand = 7;
    public const int RHand = 8;
    public const int LArm = 9;
    public const int RArm = 10;
    public const int Waist = 11;
    public const int Leg = 12;
    public const int Foot = 13;
    public const int FirstAcc = 14;
    public const int OverCoat = 15;
    public const int OverHelm = 16;
    public const int SecondAcc = 17;
    public const int ThirdAcc = 18;

    public static string ItemSlotMetaValuesStoresBank(int slot)
    {
        return slot switch
        {
            0 => "None",
            1 => "Weapons",
            2 => "Armors",
            3 => "Shields",
            4 => "Helmets",
            5 => "Earrings",
            6 => "Amulets",
            7 => "Rings",
            8 => "Rings",
            9 => "Arms",
            10 => "Arms",
            11 => "Belts",
            12 => "Leggings",
            13 => "Boots",
            14 => "Other",
            15 => "Overcoats",
            16 => "Overhelms",
            17 => "Other",
            18 => "Other",
            _ => "Other"
        };
    }
}