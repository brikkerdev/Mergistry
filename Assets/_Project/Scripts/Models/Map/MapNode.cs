using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models.Combat;

namespace Mergistry.Models.Map
{
    public class MapNode
    {
        public int          Id;
        public MapNodeType  Type;
        public int          Row;
        public int          Col;
        public int          TotalCols;   // total columns in this row (for world-position calc)
        public List<int>    NextNodeIds; // ids of nodes in the next row this connects to
        public bool         IsVisited;
        public bool         IsAccessible;

        /// <summary>
        /// Pre-generated enemy composition for Combat/Elite/Boss nodes.
        /// Null for Event nodes.
        /// </summary>
        public CombatSetup  CombatSetup;

        public MapNode()
        {
            NextNodeIds = new List<int>();
        }
    }
}
