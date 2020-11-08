using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pacman
{
    /// <summary>
    /// Represents a Superpacpoint, derives from Pacpoint
    /// </summary>
    public class SuperPacpoint : Pacpoint
    {
        public override void OnEaten()
        {
            //Same as base
            base.OnEaten();

            if (GameBoard.Instance != null)
            {
                //But triggers the board event to scare ghosts
                if(GameBoard.Instance.onSuperPacpointEaten != null)
                    GameBoard.Instance.onSuperPacpointEaten.Invoke();
            }
        }
    }
}