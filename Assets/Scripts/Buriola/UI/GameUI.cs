using System.Collections;
using Buriola.Board;
using UnityEngine;
using UnityEngine.UI;

namespace Buriola.UI
{
    /// <summary>
    /// Class to handle UI for the game
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        #region Variables
        [SerializeField]
        private Color gameOverColor = default;
        [SerializeField]
        private Color playerInfoColor = default;

        [SerializeField]
        private Text playerText = null;
        [SerializeField]
        private Text readyText = null;

        [SerializeField]
        private Text highScoreText = null;
        [SerializeField]
        private Text playerOneScore = null;
        [SerializeField]
        private Text playerTwoScore = null;
        [SerializeField]
        private Text playerTwoTitle = null;
        [SerializeField]
        private Text ghostConsumedText = null;
        [SerializeField]
        private Text fruitConsumedText = null;

        [SerializeField]
        private Image[] pacmanLives = default;
        [SerializeField]
        private Image[] levelFruits = default;

        [SerializeField]
        private Animator playerOneUI = null;
        [SerializeField]
        private Animator playerTwoUI = null;
        #endregion

        private void Start()
        {
            playerOneScore.text = "0";
            playerTwoScore.text = "0";

            playerText.enabled = false;
            readyText.enabled = false;

            playerOneUI.SetBool("Blink", true);

            UpdatePlayerLives(3);
            SetFruitImage(1);

            SetupUI();
        }

        /// <summary>
        /// Shows a text on the screen for a time
        /// </summary>
        /// <param name="textToShow">The text you want to show</param>
        /// <param name="delay">how long it will be shown</param>
        /// <returns></returns>
        private IEnumerator ShowText(Text textToShow, float delay = 1f)
        {
            textToShow.enabled = true;
            yield return new WaitForSeconds(delay);
            textToShow.enabled = false;
        }

        /// <summary>
        /// Setup UI for One player or Two player
        /// </summary>
        public void SetupUI()
        {
            if(GameController.Instance != null)
            {
                if(GameController.isOnePlayerGame)
                {
                    //Deactivates player two texts
                    playerTwoUI.enabled = false;
                    playerTwoScore.enabled = false;
                    playerTwoTitle.enabled = false;    
                }
                else
                {
                    //Activates player two texts
                    playerTwoUI.enabled = true;
                    playerTwoScore.enabled = true;
                    playerTwoTitle.enabled = true;
                }

                //Get high score
                highScoreText.text = GameController.Instance.LoadHighScore().ToString();
            }
        }

        /// <summary>
        /// Shows the level text
        /// </summary>
        /// <param name="currentLevel">The current level</param>
        /// <param name="delay">time to show</param>
        public void ShowLevelText(int currentLevel, float delay = 0f)
        {
            //set color
            playerText.color = playerInfoColor;
            playerText.text = "Level " + currentLevel.ToString();
            //show
            StartCoroutine(ShowText(playerText, delay));
        }

        /// <summary>
        /// Show current player text
        /// </summary>
        /// <param name="currentPlayer">The current player</param>
        /// <param name="delay">time to show</param>
        public void ShowPlayerText(GameBoard.Players currentPlayer, float delay = 0f)
        {
            //Set color
            playerText.color = playerInfoColor;

            if (currentPlayer == GameBoard.Players.PlayerOne)
            {
                playerText.text = "Player 1";
                StartCoroutine(ShowText(playerText, delay));
            }
            else
            {
                playerText.text = "Player 2";
                StartCoroutine(ShowText(playerText, delay));
            }
        }

        /// <summary>
        /// Show Ready Text for an amount of time
        /// </summary>
        /// <param name="delay">Time to show</param>
        public void ShowReadyText(float delay = 0f)
        {
            StartCoroutine(ShowText(readyText, delay));
        }

        /// <summary>
        /// Show Game Over Text
        /// </summary>
        /// <param name="delay">time to show</param>
        public void ShowGameOverText(float delay = 0f)
        {
            playerText.color = gameOverColor;
            playerText.text = "Game Over";
            StartCoroutine(ShowText(playerText, delay));
        }

        /// <summary>
        /// Update the image for the player lives
        /// </summary>
        /// <param name="lives">How many lives to show</param>
        public void UpdatePlayerLives(int lives)
        {
            for (int i = 0; i < pacmanLives.Length; i++)
            {
                pacmanLives[i].enabled = false;
            }

            for (int i = 0; i < lives; i++)
            {
                pacmanLives[i].enabled = true;
            }
        }

        /// <summary>
        /// Set the bonus items grid based on the current level
        /// </summary>
        /// <param name="currentLevel">The current level</param>
        public void SetFruitImage(int currentLevel)
        {
            for (int i = 0; i < levelFruits.Length; i++)
            {
                Color c = levelFruits[i].color;
                c = new Color(c.r, c.g, c.b, .2f);
                levelFruits[i].color = c;
            }

            if(currentLevel > levelFruits.Length)
            {
                for (int i = 0; i < levelFruits.Length; i++)
                {
                    Color c = levelFruits[i].color;
                    c = new Color(c.r, c.g, c.b, 1f);
                    levelFruits[i].color = c;
                }
            }
            else
            {
                for (int i = 0; i < currentLevel; i++)
                {
                    Color c = levelFruits[i].color;
                    c = new Color(c.r, c.g, c.b, 1f);
                    levelFruits[i].color = c;
                }
            }
        }

        /// <summary>
        /// Show text score in the position where Pacman ate it
        /// </summary>
        /// <param name="pos">Position to show</param>
        /// <param name="score">Score to show</param>
        public void SetBonusItemScoreText(Vector2 pos, int score)
        {
            fruitConsumedText.text = score.ToString();

            RectTransform rectTransfrom = fruitConsumedText.GetComponent<RectTransform>();
            Vector2 viewPortPoint = Camera.main.WorldToViewportPoint(pos);
            rectTransfrom.anchorMin = viewPortPoint;
            rectTransfrom.anchorMax = viewPortPoint;

            StartCoroutine(ShowText(fruitConsumedText, 1f));
        }

        /// <summary>
        /// Show text score in the position where Pacman ate ghost
        /// </summary>
        /// <param name="pos">Position to show</param>
        /// <param name="score">Score to show</param>
        public void SetGhostEatenScoreText(Vector2 pos, int score)
        {
            ghostConsumedText.text = score.ToString();

            RectTransform rectTransfrom = ghostConsumedText.GetComponent<RectTransform>();
            Vector2 viewPortPoint = Camera.main.WorldToViewportPoint(pos);
            rectTransfrom.anchorMin = viewPortPoint;
            rectTransfrom.anchorMax = viewPortPoint;

            StartCoroutine(ShowText(ghostConsumedText, 1f));
        }

        /// <summary>
        /// Updates the current player score text
        /// </summary>
        /// <param name="currentPlayer"></param>
        /// <param name="score"></param>
        public void UpdatePlayerScore(GameBoard.Players currentPlayer, int score)
        {
            if (currentPlayer == GameBoard.Players.PlayerOne)
            {
                playerOneScore.text = score.ToString();
            }
            else
            {
                playerTwoScore.text = score.ToString();
            }
        }

        /// <summary>
        /// Swaps active animator to show who is currently playing
        /// </summary>
        /// <param name="currentPlayer"></param>
        public void SwapPlayers(GameBoard.Players currentPlayer)
        {
            if(currentPlayer == GameBoard.Players.PlayerOne)
            {
                playerOneUI.SetBool("Blink", true);
                playerTwoUI.SetBool("Blink", false);
            }
            else
            {
                playerOneUI.SetBool("Blink", false);
                playerTwoUI.SetBool("Blink", true);
            }
        }
    }
}
