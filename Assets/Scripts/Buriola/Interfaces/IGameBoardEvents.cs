using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pacman.Interfaces
{
    /// <summary>
    /// Interface to represent the board events. Ghosts and Pacman must implement this interface to be able to respond
    /// to the board events
    /// </summary>
    public interface IGameBoardEvents 
    {
        void OnGameStart();
        void OnAfterGameStart();
        void OnGameBoardRestart();
        void OnAfterGameBoardRestart();
    }
}