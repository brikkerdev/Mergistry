using Mergistry.Data;
using UnityEngine;

namespace Mergistry.Services
{
    public class LootService : ILootService
    {
        private static readonly PotionType[] _lootPool = new[]
        {
            PotionType.Flame, PotionType.Stream, PotionType.Poison,
            PotionType.Steam, PotionType.Napalm, PotionType.Acid,
            PotionType.Radiance, PotionType.Gloom,
            PotionType.Lightning, PotionType.Flare, PotionType.Spore,
            PotionType.Curse, PotionType.Mist, PotionType.Miasma
        };

        public PotionType GetRandomPotionType()
        {
            return _lootPool[Random.Range(0, _lootPool.Length)];
        }
    }
}
