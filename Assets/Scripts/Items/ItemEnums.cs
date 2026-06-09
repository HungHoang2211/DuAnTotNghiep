namespace SimpleSurvival.Items
{
    public enum WeaponModSlot
    {
        Magazine,
        Barrel,
        Scope,
        Accessory,
        Stock,
        Head,
        Handle,
    }

    public enum ToolType
    {
        Axe,
        Pickaxe
    }

    public enum EquipSlot
    {
        None = -1,
        Weapon,
        Backpack,
        Helmet,
        Jacket,
        Pants,
        Boots,
        QuickSlot,
    }

    public enum WeaponCategory
    {
        Fists,
        Melee1H,
        Melee2H,
        Pistol,
        Rifle
    }

    [System.Flags]
    public enum ItemTag
    {
        None = 0,
        Resource = 1 << 0,
        Consumable = 1 << 1,
        Weapon = 1 << 2,
        Tool = 1 << 3,
        Helmet = 1 << 4,
        Jacket = 1 << 5,
        Pants = 1 << 6,
        Boots = 1 << 7,
        Backpack = 1 << 8,
        QuickSlot = 1 << 9,
    }
}