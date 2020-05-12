using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.GameObject;
using HexWork.Gameplay.GameObject.Characters;

namespace HexWork.Gameplay
{
    public class GameState : HexGrid
    {
        #region Attributes

        //list of all characters in the current match ordered by initiative count
        public IEnumerable<Character> Characters => Entities.OfType<Character>();
        public IEnumerable<TileEffect> TileEffects => Entities.OfType<TileEffect>();

        public List<HexGameObject> Entities = new List<HexGameObject>();

        public int MaxPotential = 9;
        public int Potential = 3;

        #endregion

        #region Properties

        public Character ActiveCharacter => Characters.OrderBy(c => c.TurnTimer).FirstOrDefault();

        public IEnumerable<Character> Heroes => LivingCharacters.Where(character => character.IsHero);
        public IEnumerable<Character> LivingCharacters => Characters.Where(c => c.IsAlive);
        public IEnumerable<Character> Enemies => Characters.Where(character => !character.IsHero && character.IsAlive);

        #endregion
    }
}
