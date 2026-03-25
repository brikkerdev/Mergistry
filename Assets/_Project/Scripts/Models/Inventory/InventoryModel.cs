using Mergistry.Data;

namespace Mergistry.Models
{
    public class InventoryModel
    {
        public const int SlotCount = 4;

        private readonly PotionSlot[] _slots;

        public InventoryModel()
        {
            _slots = new PotionSlot[SlotCount];
            for (int i = 0; i < SlotCount; i++)
                _slots[i] = PotionSlot.Empty();
        }

        public PotionSlot GetSlot(int index) => _slots[index];

        /// <summary>
        /// Try to add a brew into any empty slot.
        /// Returns true if placed; false if all slots full (caller shows popup).
        /// </summary>
        public bool TryAdd(PotionType type, int level)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    _slots[i].Type  = type;
                    _slots[i].Level = level;
                    return true;
                }
            }
            return false;
        }

        public void Replace(int slotIndex, PotionType type, int level)
        {
            _slots[slotIndex].Type              = type;
            _slots[slotIndex].Level             = level;
            _slots[slotIndex].CooldownRemaining = 0;
        }

        /// <summary>Returns true if every slot is empty (no potions at all).</summary>
        public bool IsEmpty()
        {
            for (int i = 0; i < SlotCount; i++)
                if (!_slots[i].IsEmpty) return false;
            return true;
        }

        /// <summary>Resets all slots to empty (used when starting a new run).</summary>
        public void Clear()
        {
            for (int i = 0; i < SlotCount; i++)
                _slots[i] = PotionSlot.Empty();
        }
    }
}
