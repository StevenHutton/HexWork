using System.Threading.Tasks;
using HexWork.Gameplay;
using HexWork.Gameplay.Actions;
using HexWork.UI.Interfaces;

namespace HexWork.UI
{
    public class DummyInputProvider : IInputProvider
    {
        private HexCoordinate _coord;

        public DummyInputProvider(HexCoordinate coordinate)
        {
            _coord = coordinate;
        }

        public async Task<HexCoordinate> GetTargetAsync(HexAction action)
        {
            return _coord;
        }
    }
}
