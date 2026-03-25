namespace Mergistry.Data
{
    /// <summary>
    /// All data needed to display a potion in the recipe book.
    /// </summary>
    [System.Serializable]
    public struct PotionEntry
    {
        public PotionType  Type;
        public string      Name;           // Display name (Russian)
        public ElementType IngredientA;
        public ElementType IngredientB;
        public int         DamagePerLevel; // multiplied by potion level
        public string      AoEPattern;     // Short label for AoE shape
        public string      EffectDesc;     // Brief effect description
        public bool        IsBase;         // true = same-element brew
    }

    /// <summary>
    /// Static registry of all 15 potion definitions (A1: 5 base + 10 recipe).
    /// </summary>
    public static class PotionDatabase
    {
        private static readonly ElementType Ig = ElementType.Ignis;
        private static readonly ElementType Aq = ElementType.Aqua;
        private static readonly ElementType Tx = ElementType.Toxin;
        private static readonly ElementType Lx = ElementType.Lux;
        private static readonly ElementType Um = ElementType.Umbra;

        public static readonly PotionEntry[] All =
        {
            // ── Base brews (same element) ─────────────────────────────────────
            new PotionEntry { Type = PotionType.Flame,     Name = "Пламя",     IngredientA = Ig, IngredientB = Ig, DamagePerLevel = 3, AoEPattern = "Крест ×1",      EffectDesc = "Поджигает зону",         IsBase = true  },
            new PotionEntry { Type = PotionType.Stream,    Name = "Поток",     IngredientA = Aq, IngredientB = Aq, DamagePerLevel = 3, AoEPattern = "Линия 4",       EffectDesc = "Смывает линию",          IsBase = true  },
            new PotionEntry { Type = PotionType.Poison,    Name = "Яд",        IngredientA = Tx, IngredientB = Tx, DamagePerLevel = 2, AoEPattern = "Блок 2×2",      EffectDesc = "Отравляет область",      IsBase = true  },
            new PotionEntry { Type = PotionType.Radiance,  Name = "Сияние",    IngredientA = Lx, IngredientB = Lx, DamagePerLevel = 1, AoEPattern = "Блок 3×3",      EffectDesc = "Ослепляет (A3)",         IsBase = true  },
            new PotionEntry { Type = PotionType.Gloom,     Name = "Мрак",      IngredientA = Um, IngredientB = Um, DamagePerLevel = 5, AoEPattern = "Одна цель",     EffectDesc = "Проклинает (A3)",        IsBase = true  },
            // ── Recipe brews (mixed elements) ────────────────────────────────
            new PotionEntry { Type = PotionType.Steam,     Name = "Пар",       IngredientA = Aq, IngredientB = Ig, DamagePerLevel = 0, AoEPattern = "Блок 2×2",      EffectDesc = "Замедляет (A3)",         IsBase = false },
            new PotionEntry { Type = PotionType.Napalm,    Name = "Напалм",    IngredientA = Ig, IngredientB = Tx, DamagePerLevel = 3, AoEPattern = "Крест ×1",      EffectDesc = "Зона огня",              IsBase = false },
            new PotionEntry { Type = PotionType.Acid,      Name = "Кислота",   IngredientA = Aq, IngredientB = Tx, DamagePerLevel = 2, AoEPattern = "Блок 2×2",      EffectDesc = "Снимает броню (A2)",     IsBase = false },
            new PotionEntry { Type = PotionType.Lightning, Name = "Молния",    IngredientA = Aq, IngredientB = Lx, DamagePerLevel = 3, AoEPattern = "Линия 5",       EffectDesc = "Цепная атака",           IsBase = false },
            new PotionEntry { Type = PotionType.Flare,     Name = "Вспышка",   IngredientA = Ig, IngredientB = Lx, DamagePerLevel = 3, AoEPattern = "Блок 3×3",      EffectDesc = "Оглушает (A3)",          IsBase = false },
            new PotionEntry { Type = PotionType.Spore,     Name = "Спора",     IngredientA = Tx, IngredientB = Lx, DamagePerLevel = 3, AoEPattern = "Крест ×1",      EffectDesc = "DoT-яд (A3)",            IsBase = false },
            new PotionEntry { Type = PotionType.Curse,     Name = "Проклятие", IngredientA = Ig, IngredientB = Um, DamagePerLevel = 0, AoEPattern = "Одна цель",     EffectDesc = "Проклинает (A3)",        IsBase = false },
            new PotionEntry { Type = PotionType.Mist,      Name = "Туман",     IngredientA = Aq, IngredientB = Um, DamagePerLevel = 0, AoEPattern = "Одна цель",     EffectDesc = "Слепит и тормозит (A3)", IsBase = false },
            new PotionEntry { Type = PotionType.Miasma,    Name = "Миазм",     IngredientA = Tx, IngredientB = Um, DamagePerLevel = 2, AoEPattern = "Блок 3×3",      EffectDesc = "DoT-аура (A3)",          IsBase = false },
            new PotionEntry { Type = PotionType.Chaos,     Name = "Хаос",      IngredientA = Lx, IngredientB = Um, DamagePerLevel = 2, AoEPattern = "Случайная",     EffectDesc = "Случайный эффект",       IsBase = false },
        };

        /// <summary>Short two-letter element abbreviation for display in the recipe book.</summary>
        public static string ElementAbbr(ElementType e) => e switch
        {
            ElementType.Ignis => "Ig",
            ElementType.Aqua  => "Aq",
            ElementType.Toxin => "Tx",
            ElementType.Lux   => "Lx",
            ElementType.Umbra => "Um",
            _                 => "?"
        };
    }
}
