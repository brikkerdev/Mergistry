using System.Collections.Generic;
using Mergistry.Data;

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

        public MapNode()
        {
            NextNodeIds = new List<int>();
        }
    }
}
