using System.Collections.Generic;
using Mergistry.Data;

namespace Mergistry.Models
{
    /// <summary>
    /// Tracks active relics for the current run.
    /// </summary>
    public class RelicModel
    {
        private readonly List<RelicType> _relics = new List<RelicType>();

        public IReadOnlyList<RelicType> ActiveRelics => _relics;

        public bool Has(RelicType type) => _relics.Contains(type);

        public void Add(RelicType type)
        {
            if (!_relics.Contains(type))
                _relics.Add(type);
        }

        public void Clear() => _relics.Clear();
    }
}
