using System.Collections.Generic;
using System.Linq;

namespace Mergistry.Models.Map
{
    public class FloorMapModel
    {
        public List<MapNode> Nodes     { get; } = new List<MapNode>();
        public int           TotalRows { get; set; }

        public MapNode       GetNode(int id)  => Nodes.FirstOrDefault(n => n.Id == id);
        public List<MapNode> GetRow(int row)  => Nodes.Where(n => n.Row == row).OrderBy(n => n.Col).ToList();
    }
}
