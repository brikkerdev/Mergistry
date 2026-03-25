using Mergistry.Models.Combat;

namespace Mergistry.Services
{
    public interface IAIService
    {
        void DetermineIntents(CombatModel model);
        void ExecuteIntents(CombatModel model);
    }
}
