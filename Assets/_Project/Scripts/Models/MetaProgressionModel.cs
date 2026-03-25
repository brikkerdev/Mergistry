using System;
using System.Collections.Generic;

namespace Mergistry.Models
{
    /// <summary>
    /// Persistent meta-progression (across runs). Serializable via JsonUtility.
    /// </summary>
    [Serializable]
    public class MetaProgressionModel
    {
        public int             Version          = 1;
        public int             TotalRuns        = 0;
        public int             TotalVictories   = 0;
        public int             BestFightReached = 0;
        public bool            TutorialCompleted = false;

        // Lists of unlocked recipe/relic indices (int IDs)
        public List<int>       UnlockedRecipes  = new List<int>();
        public List<int>       UnlockedRelics   = new List<int>();
    }
}
