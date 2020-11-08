using System.Collections;
using Buriola.Board;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Buriola.UI
{
    public class GameUI : MonoBehaviour
    {
        #region Variables
        [FormerlySerializedAs("gameOverColor")] 
        [SerializeField]
        private Color _gameOverColor = default;
        [FormerlySerializedAs("playerInfoColor")] 
        [SerializeField]
        private Color _playerInfoColor = default;

        [FormerlySerializedAs("playerText")] 
        [SerializeField]
        private Text _playerText = null;
        [FormerlySerializedAs("readyText")] 
        [SerializeField]
        private Text _readyText = null;

        [FormerlySerializedAs("highScoreText")] 
        [SerializeField]
        private Text _highScoreText = null;
        [FormerlySerializedAs("playerOneScore")] 
        [SerializeField]
        private Text _playerOneScore = null;
        [FormerlySerializedAs("playerTwoScore")] 
        [SerializeField]
        private Text _playerTwoScore = null;
        [FormerlySerializedAs("playerTwoTitle")]
        [SerializeField]
        private Text _playerTwoTitle = null;
        [FormerlySerializedAs("ghostConsumedText")] 
        [SerializeField]
        private Text _ghostConsumedText = null;
        [FormerlySerializedAs("fruitConsumedText")]
        [SerializeField]
        private Text _fruitConsumedText = null;

        [FormerlySerializedAs("pacmanLives")]
        [SerializeField]
        private Image[] _pacmanLives = default;
        [FormerlySerializedAs("levelFruits")] 
        [SerializeField]
        private Image[] _levelFruits = default;

        [FormerlySerializedAs("playerOneUI")] 
        [SerializeField]
        private Animator _playerOneUI = null;
        [FormerlySerializedAs("playerTwoUI")] 
        [SerializeField]
        private Animator _playerTwoUI = null;

        private static readonly int Blink = Animator.StringToHash("Blink");

        #endregion

        private void Start()
        {
            _playerOneScore.text = "0";
            _playerTwoScore.text = "0";

            _playerText.enabled = false;
            _readyText.enabled = false;

            _playerOneUI.SetBool(Blink, true);

            UpdatePlayerLives(3);
            SetFruitImage(1);

            SetupUI();
        }

        private IEnumerator ShowText(Text textToShow, float delay = 1f)
        {
            textToShow.enabled = true;
            yield return new WaitForSeconds(delay);
            textToShow.enabled = false;
        }

        public void SetupUI()
        {
            if(GameController.Instance != null)
            {
                if(GameController.IsOnePlayerGame)
                {
                    _playerTwoUI.enabled = false;
                    _playerTwoScore.enabled = false;
                    _playerTwoTitle.enabled = false;    
                }
                else
                {
                    _playerTwoUI.enabled = true;
                    _playerTwoScore.enabled = true;
                    _playerTwoTitle.enabled = true;
                }

                _highScoreText.text = GameController.Instance.LoadHighScore().ToString();
            }
        }

        public void ShowLevelText(int currentLevel, float delay = 0f)
        {
            _playerText.color = _playerInfoColor;
            _playerText.text = "Level " + currentLevel.ToString();
            
            StartCoroutine(ShowText(_playerText, delay));
        }

        public void ShowPlayerText(GameBoard.Players currentPlayer, float delay = 0f)
        {
            _playerText.color = _playerInfoColor;

            if (currentPlayer == GameBoard.Players.PlayerOne)
            {
                _playerText.text = "Player 1";
                StartCoroutine(ShowText(_playerText, delay));
            }
            else
            {
                _playerText.text = "Player 2";
                StartCoroutine(ShowText(_playerText, delay));
            }
        }

        public void ShowReadyText(float delay = 0f)
        {
            StartCoroutine(ShowText(_readyText, delay));
        }

        public void ShowGameOverText(float delay = 0f)
        {
            _playerText.color = _gameOverColor;
            _playerText.text = "Game Over";
            StartCoroutine(ShowText(_playerText, delay));
        }

        public void UpdatePlayerLives(int lives)
        {
            for (int i = 0; i < _pacmanLives.Length; i++)
            {
                _pacmanLives[i].enabled = false;
            }

            for (int i = 0; i < lives; i++)
            {
                _pacmanLives[i].enabled = true;
            }
        }

        public void SetFruitImage(int currentLevel)
        {
            for (int i = 0; i < _levelFruits.Length; i++)
            {
                Color c = _levelFruits[i].color;
                c = new Color(c.r, c.g, c.b, .2f);
                _levelFruits[i].color = c;
            }

            if(currentLevel > _levelFruits.Length)
            {
                for (int i = 0; i < _levelFruits.Length; i++)
                {
                    Color c = _levelFruits[i].color;
                    c = new Color(c.r, c.g, c.b, 1f);
                    _levelFruits[i].color = c;
                }
            }
            else
            {
                for (int i = 0; i < currentLevel; i++)
                {
                    Color c = _levelFruits[i].color;
                    c = new Color(c.r, c.g, c.b, 1f);
                    _levelFruits[i].color = c;
                }
            }
        }

        public void SetBonusItemScoreText(Vector2 pos, int score)
        {
            _fruitConsumedText.text = score.ToString();

            RectTransform rectTransfrom = _fruitConsumedText.GetComponent<RectTransform>();
            Vector2 viewPortPoint = Camera.main.WorldToViewportPoint(pos);
            rectTransfrom.anchorMin = viewPortPoint;
            rectTransfrom.anchorMax = viewPortPoint;

            StartCoroutine(ShowText(_fruitConsumedText, 1f));
        }

        public void SetGhostEatenScoreText(Vector2 pos, int score)
        {
            _ghostConsumedText.text = score.ToString();

            RectTransform rectTransfrom = _ghostConsumedText.GetComponent<RectTransform>();
            Vector2 viewPortPoint = Camera.main.WorldToViewportPoint(pos);
            rectTransfrom.anchorMin = viewPortPoint;
            rectTransfrom.anchorMax = viewPortPoint;

            StartCoroutine(ShowText(_ghostConsumedText, 1f));
        }

        public void UpdatePlayerScore(GameBoard.Players currentPlayer, int score)
        {
            if (currentPlayer == GameBoard.Players.PlayerOne)
            {
                _playerOneScore.text = score.ToString();
            }
            else
            {
                _playerTwoScore.text = score.ToString();
            }
        }

        public void SwapPlayers(GameBoard.Players currentPlayer)
        {
            if(currentPlayer == GameBoard.Players.PlayerOne)
            {
                _playerOneUI.SetBool(Blink, true);
                _playerTwoUI.SetBool(Blink, false);
            }
            else
            {
                _playerOneUI.SetBool(Blink, false);
                _playerTwoUI.SetBool(Blink, true);
            }
        }
    }
}
