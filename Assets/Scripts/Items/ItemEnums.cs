namespace SimpleSurvival.Items
{
    /// <summary>
    /// Attachment points on a weapon that can hold a visual mod mesh.
    /// The character rig keeps one SkinnedMeshRenderer per slot.
    /// Each WeaponAbility declares only the slots its weapon physically has —
    /// a knife lists Head + Handle; a rifle lists all five ranged slots.
    /// </summary>
    public enum WeaponModSlot
    {
        // ── Ranged ──────────────────────────────
        Magazine,    // băng đạn
        Barrel,      // nòng súng (bao gồm giảm thanh nếu gắn liền)
        Scope,       // ống ngắm
        Accessory,   // phụ kiện (đèn pin, laser, tay cầm phụ,...)
        Stock,       // báng súng

        // ── Melee ───────────────────────────────
        Head,        // lưỡi / đầu búa / bộ phận tấn công
        Handle,      // tay cầm / chuôi
    }

    /// <summary>What kind of resource a tool is effective against.</summary>
    public enum ToolType
    {
        Axe,
        Pickaxe
    }

    /// <summary>Equipment slots a piece of gear can occupy.</summary>
    public enum EquipSlot
    {
        Weapon,
        Backpack,
        Helmet,
        Jacket,
        Pants,
        Boots
    }

    /// <summary>
    /// Categorises an item's role. Multiple tags can be combined —
    /// a stone axe carries both Tool and Weapon.
    /// Equipment slots use these tags to decide which items they accept.
    /// </summary>
    [System.Flags]
    public enum ItemTag
    {
        None = 0,
        Resource = 1 << 0,
        Food = 1 << 1,
        Weapon = 1 << 2,
        Tool = 1 << 3,
        Helmet = 1 << 4,
        Jacket = 1 << 5,
        Pants = 1 << 6,
        Boots = 1 << 7,
        Backpack = 1 << 8,
    }
}