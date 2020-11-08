using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pacman
{
    [System.Serializable]
    ///Class to represent player data
    public class PlayerStats
    {
        private int pacmanLives; //Lives
        public int CurrentLevel { get; set; } //current level
        public bool BonusItemShown { get; set; } //flag to show bonus items

        public PlayerStats(int currentLevel, int pacmanLives, int score)
        {
            CurrentLevel = currentLevel;
            this.pacmanLives = pacmanLives;
            Score = score;
        }

        public int Score { get; set; } //score
        public int PacpointsConsumed { get; set; } //pacpoints consumed

        /// <summary>
        /// Called on this player to lose a life
        /// </summary>
        public void LoseLife()
        {
            pacmanLives--;
            if (pacmanLives <= 0)
                pacmanLives = 0;
        }

        /// <summary>
        /// Checks to see if it is game over for this player
        /// </summary>
        /// <returns></returns>
        public bool GameOver()
        {
            return pacmanLives == 0;
        }

        /// <summary>
        /// How many lives you hvae
        /// </summary>
        /// <returns></returns>
        public int GetLives()
        {
            return pacmanLives;
        }
    }
}