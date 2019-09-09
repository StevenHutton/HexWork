using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace HexWork
{
    public class PlayerIndexEventArgs : EventArgs
    {
        private PlayerIndex _playerIndex;

        public PlayerIndex PlayerIndex
        {
            get { return _playerIndex; }            
        }

        public PlayerIndexEventArgs(PlayerIndex pIndex)
        {
            _playerIndex = pIndex;
        }
    }
}
