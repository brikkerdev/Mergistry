using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models;

namespace Mergistry.Services
{
    public interface IDistillationService
    {
        BoardModel GenerateBoard(int seed = 0, int floor = 0);
        bool CanMerge(BoardModel board, int fromX, int fromY, int toX, int toY);
        (PotionType potionType, ElementType element) PerformMerge(BoardModel board, int fromX, int fromY, int toX, int toY);
        bool CanInfuse(BoardModel board, int fromX, int fromY, int toX, int toY);
        int PerformInfuse(BoardModel board, int fromX, int fromY, int toX, int toY);
        List<DistillationService.BrewEntry> CollectBrews(BoardModel board);
    }
}
