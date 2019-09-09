using System.Threading.Tasks;
using HexWork.Gameplay;
using HexWork.Gameplay.Actions;

namespace HexWork.UI.Interfaces
{
    public interface IInputProvider
    {
        Task<HexCoordinate> GetTargetAsync(HexAction action);
    }
}
