namespace Buriola.Board.Data
{
    [System.Serializable]
    public class PlayerStats
    {
        private int _pacmanLives; //Lives
        public int CurrentLevel { get; set; } //current level
        public bool BonusItemShown { get; set; } //flag to show bonus items

        public PlayerStats(int currentLevel, int pacmanLives, int score)
        {
            CurrentLevel = currentLevel;
            _pacmanLives = pacmanLives;
            Score = score;
        }

        public int Score { get; set; } //score
        public int PacpointsConsumed { get; set; } //pacpoints consumed

        /// <summary>
        /// Called on this player to lose a life
        /// </summary>
        public void LoseLife()
        {
            _pacmanLives--;
            if (_pacmanLives <= 0)
                _pacmanLives = 0;
        }

        /// <summary>
        /// Checks to see if it is game over for this player
        /// </summary>
        /// <returns></returns>
        public bool GameOver()
        {
            return _pacmanLives == 0;
        }

        /// <summary>
        /// How many lives you hvae
        /// </summary>
        /// <returns></returns>
        public int GetLives()
        {
            return _pacmanLives;
        }
    }
}
