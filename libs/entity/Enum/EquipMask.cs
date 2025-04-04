using System;

namespace ACE.Entity.Enum;

/// <summary>
/// This data is sent as loc in the player description message F7B0 -0013
/// </summary>
[Flags]
public enum EquipMask : uint
{
    None = 0x00000000,
    HeadWear = 0x00000001,
    ChestWear = 0x00000002,
    AbdomenWear = 0x00000004,
    UpperArmWear = 0x00000008,
    LowerArmWear = 0x00000010,
    HandWear = 0x00000020,
    UpperLegWear = 0x00000040,
    LowerLegWear = 0x00000080,
    FootWear = 0x00000100,
    ChestArmor = 0x00000200,
    AbdomenArmor = 0x00000400,
    UpperArmArmor = 0x00000800,
    LowerArmArmor = 0x00001000,
    UpperLegArmor = 0x00002000,
    LowerLegArmor = 0x00004000,
    NeckWear = 0x00008000,
    WristWearLeft = 0x00010000,
    WristWearRight = 0x00020000,
    FingerWearLeft = 0x00040000,
    FingerWearRight = 0x00080000,
    MeleeWeapon = 0x00100000,
    Shield = 0x00200000,
    MissileWeapon = 0x00400000,
    MissileAmmo = 0x00800000,
    Held = 0x01000000,
    TwoHanded = 0x02000000,
    TrinketOne = 0x04000000,
    Cloak = 0x08000000,
    SigilOne = 0x10000000,
    SigilTwo = 0x20000000,
    SigilThree = 0x40000000,
    Clothing = 0x80000000 | HeadWear | ChestWear | AbdomenWear | UpperArmWear | LowerArmWear | HandWear | UpperLegWear | LowerLegWear | FootWear,
    Armor = ChestArmor | AbdomenArmor | UpperArmArmor | LowerArmArmor | UpperLegArmor | LowerLegArmor | FootWear | HandWear | HeadWear,
    Weapon = MeleeWeapon | MissileWeapon | TwoHanded | Held,
    WeaponAndArmor = Weapon | Armor,
    WeaponAndShield = Weapon | Shield,
    Extremity = HeadWear | HandWear | FootWear,
    Jewelry = NeckWear | WristWearLeft | WristWearRight | FingerWearLeft | FingerWearRight,
    WristWear = WristWearLeft | WristWearRight,
    FingerWear = FingerWearLeft | FingerWearRight,
    WristAndArmor = WristWearLeft | WristWearRight | Armor,
    Sigil = SigilOne | SigilTwo | SigilThree,
    Selectable = MeleeWeapon | Shield | MissileWeapon | Held | TwoHanded,
    SelectablePlusAmmo = Selectable | MissileAmmo,
    All = 0x7FFFFFFF,
    CanGoInReadySlot = 0x7FFFFFFF
}
