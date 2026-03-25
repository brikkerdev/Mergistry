using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models;

namespace Mergistry.Services
{
    public interface IRelicService
    {
        bool HasRelic(RelicType type);
        void AcquireRelic(RelicType type);
        IReadOnlyList<RelicType> GetActiveRelics();
        List<RelicType> GetRandomRelicChoices(int count);
        void SetModel(RelicModel model);
    }
}
