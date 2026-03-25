namespace Mergistry.Data
{
    public enum PotionType
    {
        None,
        // ── Base brews (same element) ─────────────────────────────────────────
        Flame,     // Ignis + Ignis
        Stream,    // Aqua + Aqua
        Poison,    // Toxin + Toxin
        Radiance,  // Lux + Lux       (A1)
        Gloom,     // Umbra + Umbra   (A1)
        // ── Recipe brews (mixed elements) ────────────────────────────────────
        Steam,     // Aqua + Ignis
        Napalm,    // Ignis + Toxin
        Acid,      // Aqua + Toxin
        Lightning, // Aqua + Lux      (A1)
        Flare,     // Ignis + Lux     (A1) — Вспышка
        Spore,     // Toxin + Lux     (A1) — Спора
        Curse,     // Ignis + Umbra   (A1) — Проклятие
        Mist,      // Aqua + Umbra    (A1) — Туман
        Miasma,    // Toxin + Umbra   (A1) — Миазм
        Chaos,     // Lux + Umbra     (A1) — Хаос
    }
}
