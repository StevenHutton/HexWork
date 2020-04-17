using System.Collections.Generic;
using System.Linq;
using HexWork.Gameplay.Characters;

namespace HexWork.Gameplay
{
    public class GameState : HexGrid
    {
        #region Attributes

        //list of all characters in the current match ordered by initiative count
        public List<Character> Characters = new List<Character>();

        public List<TileEffect> TileEffects { get; set; } = new List<TileEffect>();

        public int Difficulty = 0;
        public int MaxPotential = 11;
        public int Potential = 6;

        #endregion

        #region Properties

        public Character ActiveCharacter => Characters.First();
        public IEnumerable<Character> Heroes => LivingCharacters.Where(character => character.IsHero);
        public IEnumerable<Character> LivingCharacters => Characters.Where(c => c.IsAlive);
        public IEnumerable<Character> Enemies => Characters.Where(character => !character.IsHero && character.IsAlive);

        #endregion
    }
}
