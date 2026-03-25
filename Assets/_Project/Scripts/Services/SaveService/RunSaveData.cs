using System;
using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models;
using Mergistry.Models.Map;

namespace Mergistry.Services
{
    /// <summary>
    /// Serializable snapshot of mid-run state. Written to run.json.
    /// </summary>
    [Serializable]
    public class RunSaveData
    {
        public int  Version       = 1;
        public int  CurrentFloor  = 0;
        public int  CurrentFight  = 0;
        public int  CurrentNodeId = -1;
        public int  PersistentHP  = 5;
        public int  MaxHP         = 5;

        // Active relics (list of enum int values)
        public List<int> ActiveRelics = new List<int>();

        // Inventory: up to 4 slots — each entry is PotionType (int) + level + cooldown
        public List<PotionSlotSaveData> InventorySlots = new List<PotionSlotSaveData>();

        // Map node visited flags: serialised as parallel arrays
        public List<int>  MapNodeIds     = new List<int>();
        public List<bool> MapNodeVisited = new List<bool>();
    }

    [Serializable]
    public class PotionSlotSaveData
    {
        public int  PotionTypeInt = -1;   // -1 = empty
        public int  Level         = 1;
        public int  Cooldown      = 0;
    }
}
