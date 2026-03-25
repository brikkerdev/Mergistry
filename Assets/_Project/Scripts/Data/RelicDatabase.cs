namespace Mergistry.Data
{
    [System.Serializable]
    public struct RelicEntry
    {
        public RelicType Type;
        public string    Name;
        public string    Description;
    }

    public static class RelicDatabase
    {
        public static readonly RelicEntry[] All = new[]
        {
            new RelicEntry { Type = RelicType.Thermos, Name = "Термос",  Description = "25% шанс не тратить кулдаун зелья" },
            new RelicEntry { Type = RelicType.Lens,    Name = "Линза",   Description = "AoE зелий увеличен на 1" },
            new RelicEntry { Type = RelicType.Flask,   Name = "Фляга",   Description = "+1 HP после каждого боя" },
            new RelicEntry { Type = RelicType.Cube,    Name = "Кубик",   Description = "+1 действие перегонки" },
            new RelicEntry { Type = RelicType.Prism,   Name = "Призма",  Description = "Урон combo ×1.5" },
        };

        public static RelicEntry Get(RelicType type)
        {
            foreach (var e in All)
                if (e.Type == type) return e;
            return default;
        }
    }
}
