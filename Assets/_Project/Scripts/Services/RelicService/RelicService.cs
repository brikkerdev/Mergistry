using System.Collections.Generic;
using System.Linq;
using Mergistry.Core;
using Mergistry.Data;
using Mergistry.Events;
using Mergistry.Models;
using UnityEngine;

namespace Mergistry.Services
{
    public class RelicService : IRelicService
    {
        private RelicModel _model;

        public void SetModel(RelicModel model) => _model = model;

        public bool HasRelic(RelicType type) => _model != null && _model.Has(type);

        public void AcquireRelic(RelicType type)
        {
            if (_model == null) return;
            _model.Add(type);
            EventBus.Publish(new RelicAcquiredEvent { Type = type });
            Debug.Log($"[RelicService] Acquired relic: {type}");
        }

        public IReadOnlyList<RelicType> GetActiveRelics() =>
            _model?.ActiveRelics ?? (IReadOnlyList<RelicType>)new List<RelicType>();

        /// <summary>
        /// Returns `count` random relics that the player does NOT already own.
        /// </summary>
        public List<RelicType> GetRandomRelicChoices(int count)
        {
            var all = new List<RelicType>
            {
                RelicType.Thermos, RelicType.Lens, RelicType.Flask,
                RelicType.Cube, RelicType.Prism
            };

            // Remove already owned
            if (_model != null)
                all.RemoveAll(r => _model.Has(r));

            // Shuffle (Fisher-Yates)
            for (int i = all.Count - 1; i > 0; i--)
            {
                int j    = Random.Range(0, i + 1);
                var temp = all[i];
                all[i]   = all[j];
                all[j]   = temp;
            }

            return all.Take(Mathf.Min(count, all.Count)).ToList();
        }
    }
}
