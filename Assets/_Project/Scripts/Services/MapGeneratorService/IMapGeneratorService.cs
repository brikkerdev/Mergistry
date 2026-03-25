using Mergistry.Models.Map;

namespace Mergistry.Services
{
    public interface IMapGeneratorService
    {
        FloorMapModel GenerateFloor(int floor);
    }
}
