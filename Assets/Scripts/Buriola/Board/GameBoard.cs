using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Buriola.UI;
using Buriola.Player;
using Buriola.AI;
using Buriola.Board.Data;
using Buriola.Pickups;
using System;
using UnityEngine.Serialization;

namespace Buriola.Board
{
    /// <summary>
    /// Main controller of the game loop
    /// </summary>
    public class GameBoard : MonoBehaviour
    {
        #region Singleton
        private static GameBoard instance;
        public static GameBoard Instance => instance;

        #endregion

        #region Delegates
        public event Action OnGameStart;
        public event Action OnAfterGameStart;
        public event Action OnGameRestart;
        public event Action OnAfterGameRestart;
        public event Action OnGhostEaten;
        public event Action OnAfterGhostEaten;
        public event Action<BonusItem> OnBonusItemEaten;
        public event Action OnSuperPacpointEaten;
        public event Action OnLevelWin;
        public event Action OnPacmanDied;
        public event Action OnGhostReachedHouse;
        #endregion

        #region Variables
        private PacmanController _pacman;
        private int _totalPacpoints;

        public enum GameState { NewGame, InGame, Restart, GameOver }
        [FormerlySerializedAs("gameState")] 
        public GameState CurrentGameState;

        private static int _boardWidth = 28;
        private static int _boardHeight = 36;
        private readonly Node[,] _board = new Node[_boardWidth, _boardHeight];

        private bool _processWinLevel;
        private bool _multiplayer;
        private bool _showBonusItem1;
        private bool _showBonusItem2;

        public enum Players { PlayerOne = 0, PlayerTwo = 1 }
        [FormerlySerializedAs("currentPlayer")] 
        public Players CurrentPlayer;
        private PlayerStats _playerOne;
        private PlayerStats _playerTwo;
        
        private bool[] _playerOneBoardState;
        private bool[] _playerTwoBoardState;
        
        public LevelDifficulty CurrentLevelDifficulty { get; private set; }

        private readonly Dictionary<int, LevelDifficulty> _levelDifficulties = new Dictionary<int, LevelDifficulty>();
        
        [Header("Audio Settings")]
        private AudioSource _audioSource;
        [FormerlySerializedAs("backgroundSound")] [SerializeField]
        private AudioClip _backgroundSound = null;
        [FormerlySerializedAs("ghostsFrightnedSound")] [SerializeField]
        private AudioClip _ghostsFrightnedSound = null;
        [FormerlySerializedAs("ghostsRetreatSound")] [SerializeField]
        private AudioClip _ghostsRetreatSound = null;
        [FormerlySerializedAs("ghostEatenSound")] [SerializeField]
        private AudioClip _ghostEatenSound = null;
        [FormerlySerializedAs("bonusItemEaten")] [SerializeField]
        private AudioClip _bonusItemEaten = null;
        [FormerlySerializedAs("winLevelClip")] [SerializeField]
        private AudioClip _winLevelClip = null;

        [FormerlySerializedAs("bonusItems")]
        [Header("Bonus Items")]
        [SerializeField]
        private GameObject[] _bonusItems = new GameObject[8];

        [FormerlySerializedAs("gameUI")]
        [Header("UI")]
        [SerializeField]
        private GameUI _gameUI = null;
        #endregion

        #region UNITY FUNCTIONS
        private void Awake()
        {
            if (instance != null && instance != this)
                Destroy(gameObject);
            
            instance = this;
        }

        private void Start()
        {
            Init();
            StartNewGame();
        }

        private void Update()
        {
            if (WinLevel(CurrentPlayer) && !_processWinLevel)
            {
                _processWinLevel = true;
                StartCoroutine(Win());
            }
            else
            {
                ShowBonusItem();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromDelegates();
        }

        #endregion

        private void Init()
        {
            _showBonusItem1 = false;
            _showBonusItem2 = false;

            _pacman = FindObjectOfType<PacmanController>();
            _audioSource = GetComponent<AudioSource>();

            CurrentGameState = GameState.NewGame;
            CurrentPlayer = Players.PlayerOne;

            FindAllPacpoints();
            FindAllNodes();
            LoadLevelsData();
            SubscribeToDelegates();

            if (GameController.Instance != null)
                _multiplayer = !GameController.IsOnePlayerGame;

            if (_multiplayer)
            {
                _playerOne = new PlayerStats(1, 3, 0);
                _playerTwo = new PlayerStats(1, 3, 0);
            }
            else
                _playerOne = new PlayerStats(1, 3, 0);

            _gameUI.ShowPlayerText(CurrentPlayer, 1.5f);
        }

        #region GAME LOOP
        
        private void StartNewGame()
        {
            StartCoroutine(NewGame());
        }
        
        private IEnumerator NewGame()
        {
            yield return new WaitForSeconds(2.25f);
            //Sets bonus items UI
            _gameUI.SetFruitImage(GetPlayerStats(CurrentPlayer).CurrentLevel);

            //Set level difficulty
            ChangeLevelDifficulty(GetPlayerStats(CurrentPlayer));

            OnGameStart?.Invoke();

            //Show ready text
            _gameUI.ShowReadyText(1.25f);

            //If it is a multiplayer game, we save the board state for each player
            if (_multiplayer)
            {
                SaveBoardState(Players.PlayerOne);
                SaveBoardState(Players.PlayerTwo);
            }

            yield return new WaitForSeconds(2f);

            OnAfterGameStart?.Invoke();

            //Set game state
            CurrentGameState = GameState.InGame;

            //Play normal background sound
            PlayAudioClip(_backgroundSound);
        }

        public void StartRestart()
        {
            CurrentGameState = GameState.Restart;

            GetPlayerStats(CurrentPlayer).LoseLife();
            SaveBoardState(CurrentPlayer);

            _gameUI.UpdatePlayerLives(GetPlayerStats(CurrentPlayer).GetLives());

            _showBonusItem1 = false;
            _showBonusItem2 = false;

            if (GetPlayerStats(CurrentPlayer).GameOver())
            {
                CurrentGameState = GameState.GameOver;
                StartGameOver();
            }
            else
            {
                if (_multiplayer)
                {
                    SwapCurrentPlayer();

                    ChangeLevelDifficulty(GetPlayerStats(CurrentPlayer));
                    LoadBoardState(CurrentPlayer);

                    _gameUI.SwapPlayers(CurrentPlayer);
                }

                ChangeLevelDifficulty(GetPlayerStats(CurrentPlayer));
                StartCoroutine(Restart());
            }
        }
        
        private IEnumerator Restart()
        {
            _gameUI.SetFruitImage(GetPlayerStats(CurrentPlayer).CurrentLevel);
            _gameUI.ShowReadyText(1.25f);
            _gameUI.ShowPlayerText(CurrentPlayer, 1.25f);

            OnGameRestart?.Invoke();

            yield return new WaitForSeconds(1.25f);

            OnAfterGameRestart?.Invoke();

            CurrentGameState = GameState.InGame;
            PlayAudioClip(_backgroundSound);
        }

        private void StartGameOver()
        {
            CurrentGameState = GameState.GameOver;
            StartCoroutine(GameOver());
        }
        
        private IEnumerator GameOver()
        {
            _gameUI.ShowGameOverText(2f);
            yield return new WaitForSeconds(3f);

            if (_multiplayer)
            {
                Players otherPlayer = CurrentPlayer == Players.PlayerOne ? Players.PlayerTwo : Players.PlayerOne;
                if (GetPlayerStats(otherPlayer).GameOver())
                {
                    if (GameController.Instance != null)
                    {
                        GameController.Instance.SaveHighScore(GetPlayerStats(CurrentPlayer).Score);
                        GameController.Instance.SaveHighScore(GetPlayerStats(otherPlayer).Score);

                        yield return new WaitForEndOfFrame();

                        GameController.IsOnePlayerGame = true;
                        GameController.Instance.RequestSceneChange(1);
                    }
                }
                else
                {
                    GameController.Instance.SaveHighScore(GetPlayerStats(CurrentPlayer).Score);

                    CurrentGameState = GameState.Restart;
                    
                    SwapCurrentPlayer();
                    _gameUI.SetupUI();
                    _gameUI.SwapPlayers(CurrentPlayer);

                    LoadBoardState(CurrentPlayer);
                    StartCoroutine(Restart());
                }
            }
            else
            {
                if (GameController.Instance != null)
                {
                    GameController.Instance.SaveHighScore(GetPlayerStats(CurrentPlayer).Score);

                    yield return new WaitForEndOfFrame();

                    GameController.IsOnePlayerGame = true;
                    GameController.Instance.RequestSceneChange(1);
                }
            }
        }

        private bool WinLevel(Players player)
        {
            return GetPlayerStats(player).PacpointsConsumed >= _totalPacpoints;
        }
        
        private IEnumerator Win()
        {
            _audioSource.Stop();

            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = 1;

            OnLevelWin?.Invoke();

            yield return new WaitForSeconds(1f);

            _showBonusItem1 = false;
            _showBonusItem2 = false;

            GetPlayerStats(CurrentPlayer).CurrentLevel++;
            GetPlayerStats(CurrentPlayer).BonusItemShown = false;
            GetPlayerStats(CurrentPlayer).PacpointsConsumed = 0;

            ChangeLevelDifficulty(GetPlayerStats(CurrentPlayer));

            _audioSource.PlayOneShot(_winLevelClip);
            _gameUI.ShowLevelText(GetPlayerStats(CurrentPlayer).CurrentLevel, 5f);

            yield return new WaitForSeconds(5f);

            Transform pacpointParent = transform.GetChild(1);
            for (int i = 0; i < pacpointParent.childCount; i++)
            {
                pacpointParent.GetChild(i).gameObject.SetActive(true);
            }

            Transform superPacpointParent = transform.GetChild(2);
            for (int i = 0; i < superPacpointParent.childCount; i++)
            {
                superPacpointParent.GetChild(i).gameObject.SetActive(true);
            }

            SaveBoardState(CurrentPlayer);

            CurrentGameState = GameState.Restart;
            StartCoroutine(Restart());

            _processWinLevel = false;
        }

        private void SwapCurrentPlayer()
        {
            CurrentPlayer = (CurrentPlayer == Players.PlayerOne) ? Players.PlayerTwo : Players.PlayerOne;
           
            _gameUI.UpdatePlayerLives(GetPlayerStats(CurrentPlayer).GetLives());
        }

        public void UpdateScore(int value, bool isGhost = false, bool isBonusItem = false)
        {
            if (!isGhost && !isBonusItem)
                GetPlayerStats(CurrentPlayer).PacpointsConsumed++;

            GetPlayerStats(CurrentPlayer).Score += value;

            _gameUI.UpdatePlayerScore(CurrentPlayer, GetPlayerStats(CurrentPlayer).Score);
        }

        #endregion

        #region Board Data & Calculations
        private void FindAllNodes()
        {
            Node[] objects = FindObjectsOfType<Node>();

            foreach (Node n in objects)
            {
                Vector2 pos = n.transform.position;

                float x_index = Mathf.Abs(pos.x);
                float y_index = Mathf.Abs(pos.y);

                _board[(int)x_index, (int)y_index] = n;
            }
        }

        private void FindAllPacpoints()
        {
            Transform t = transform.GetChild(1);
            _totalPacpoints = t.childCount + 4;
        }

        private void SaveBoardState(Players player)
        {
            Transform pacpointsParent = transform.GetChild(1);
            Transform superPacpointsParent = transform.GetChild(2);

            if (player == Players.PlayerOne)
            {
                int lastIndex = 0;
                _playerOneBoardState = new bool[pacpointsParent.childCount + superPacpointsParent.childCount];
                for (int i = 0; i < pacpointsParent.childCount; i++)
                {
                    _playerOneBoardState[i] = pacpointsParent.GetChild(i).gameObject.activeSelf;
                    lastIndex = i;
                }

                for (int i = 0; i < superPacpointsParent.childCount; i++)
                {
                    _playerOneBoardState[lastIndex + i + 1] = superPacpointsParent.GetChild(i).gameObject.activeSelf;
                }
            }
            else
            {
                int lastIndex = 0;
                _playerTwoBoardState = new bool[pacpointsParent.childCount + superPacpointsParent.childCount];
                for (int i = 0; i < pacpointsParent.childCount; i++)
                {
                    _playerTwoBoardState[i] = pacpointsParent.GetChild(i).gameObject.activeSelf;
                    lastIndex = i;
                }

                for (int i = 0; i < superPacpointsParent.childCount; i++)
                {
                    _playerTwoBoardState[lastIndex + i + 1] = superPacpointsParent.GetChild(i).gameObject.activeSelf;
                }
            }
        }

        private void LoadBoardState(Players player)
        {
            if (_playerOneBoardState == null && _playerTwoBoardState == null)
                return;

            if (_playerOneBoardState != null && (_playerOneBoardState.Length == 0 || _playerTwoBoardState.Length == 0))
                return;

            Transform pacpointsParent = transform.GetChild(1);
            Transform superPacpointsParent = transform.GetChild(2);

            if (player == Players.PlayerOne)
            {
                int lastIndex = 0;
                for (int i = 0; i < pacpointsParent.childCount; i++)
                {
                    pacpointsParent.GetChild(i).gameObject.SetActive(_playerOneBoardState != null && _playerOneBoardState[i]);
                    lastIndex = i;
                }

                for (int i = 0; i < superPacpointsParent.childCount; i++)
                {
                    superPacpointsParent.GetChild(i).gameObject.SetActive(_playerOneBoardState != null && _playerOneBoardState[lastIndex + i + 1]);
                }
            }
            else
            {
                int lastIndex = 0;
                for (int i = 0; i < pacpointsParent.childCount; i++)
                {
                    pacpointsParent.GetChild(i).gameObject.SetActive(_playerTwoBoardState[i]);
                    lastIndex = i;
                }

                for (int i = 0; i < superPacpointsParent.childCount; i++)
                {
                    superPacpointsParent.GetChild(i).gameObject.SetActive(_playerTwoBoardState[lastIndex + i + 1]);
                }
            }
        }

        private void LoadLevelsData()
        {
            LevelDifficulty[] levelDifficulties = Resources.LoadAll<LevelDifficulty>("Data");
            if (levelDifficulties.Length > 0)
            {
                for (int i = 0; i < levelDifficulties.Length; i++)
                {
                    this._levelDifficulties.Add(i + 1, levelDifficulties[i]);
                }
            }
        }

        private void ChangeLevelDifficulty(PlayerStats playerStats)
        {
            CurrentLevelDifficulty = playerStats.CurrentLevel > _levelDifficulties.Count ? _levelDifficulties.Values.Last() : _levelDifficulties[playerStats.CurrentLevel];
        }

        public PortalNode GetPortalNodeAtPosition(Vector2 pos)
        {
            float xIndex = Mathf.Abs(pos.x);
            float yIndex = Mathf.Abs(pos.y);

            PortalNode node = _board[(int)(xIndex), (int)yIndex] as PortalNode;

            if (node != null)
            {
                if (node.PortalReceiver != null)
                    return node.PortalReceiver;
            }

            return null;
        }

        #endregion

        #region Board Events
        public void StartGhostEatenEvent(GhostAI ghostEaten)
        {
            StartCoroutine(GhostEaten(ghostEaten));
        }
        
        private IEnumerator GhostEaten(GhostAI ghostEaten)
        {
            OnGhostEaten?.Invoke();

            _audioSource.Stop();
            ghostEaten.GhostSprite.enabled = false;
            _pacman.PacmanSprite.enabled = false;

            _gameUI.SetGhostEatenScoreText(ghostEaten.transform.position, ghostEaten.PreviousScore);

            _audioSource.PlayOneShot(_ghostEatenSound);

            yield return new WaitForSeconds(1f);

            _pacman.PacmanSprite.enabled = true;
            ghostEaten.GhostSprite.enabled = true;

            OnAfterGhostEaten?.Invoke();

            PlayAudioClip(_ghostsRetreatSound);
        }

        private void StartBonusItemEaten(BonusItem item)
        {
            StartCoroutine(BonusItemEaten(item));
        }
        
        private IEnumerator BonusItemEaten(BonusItem item)
        {
            _audioSource.Stop();
            _gameUI.SetGhostEatenScoreText(item.transform.localPosition, item.ScoreValue);

            _audioSource.PlayOneShot(_bonusItemEaten);

            yield return new WaitForEndOfFrame();

            PlayAudioClip(_backgroundSound);
        }

        private void ShowBonusItem()
        {
            if (GetPlayerStats(CurrentPlayer).BonusItemShown)
                return;

            if (!_showBonusItem1 && GetPlayerStats(CurrentPlayer).PacpointsConsumed >= 70)
            {
                foreach (GameObject t in _bonusItems)
                {
                    if (t.name == CurrentLevelDifficulty.BonusItem.name)
                    {
                        t.SetActive(true);
                        _showBonusItem1 = true;
                        break;
                    }
                }
            }

            if (!_showBonusItem2 && GetPlayerStats(CurrentPlayer).PacpointsConsumed >= 170)
            {
                foreach (GameObject t in _bonusItems)
                {
                    if (t.name == CurrentLevelDifficulty.BonusItem.name)
                    {
                        t.SetActive(true);
                        _showBonusItem2 = true;
                        GetPlayerStats(CurrentPlayer).BonusItemShown = true;
                        break;
                    }
                }
            }
        }

        private void SubscribeToDelegates()
        {
            OnPacmanDied += StopSounds;
            OnSuperPacpointEaten += OnSuperPacpointEatenCallback;
            OnGhostReachedHouse += OnGhostReachedHouseCallback;
            OnBonusItemEaten += StartBonusItemEaten;
        }

        private void UnsubscribeFromDelegates()
        {
            OnPacmanDied -= StopSounds;
            OnSuperPacpointEaten -= OnSuperPacpointEatenCallback;
            OnGhostReachedHouse -= OnGhostReachedHouseCallback;
            OnBonusItemEaten -= StartBonusItemEaten;
        }

        private void OnSuperPacpointEatenCallback()
        {
            PlayAudioClip(_ghostsFrightnedSound);
        }

        private void OnGhostReachedHouseCallback()
        {
            PlayAudioClip(_backgroundSound);
        }

        public void InvokeOnPacmanDiedEvent()
        {
            OnPacmanDied?.Invoke();
        }
        
        public void InvokeSuperPacpointEvent()
        {
            OnSuperPacpointEaten?.Invoke();
        }

        public void InvokeGhostReachedHouseEvent()
        {
            OnGhostReachedHouse?.Invoke();
        }

        public void InvokeBonusItemEaten(BonusItem item)
        {
            OnBonusItemEaten?.Invoke(item);
        }
        
        #endregion

        #region Audio Related
        private void PlayAudioClip(AudioClip clip)
        {
            _audioSource.clip = clip;
            _audioSource.loop = true;
            _audioSource.Play();
        }

        private void StopSounds()
        {
            _audioSource.Stop();
        }

        #endregion

        #region Getters

        private PlayerStats GetPlayerStats(Players player)
        {
            return player == Players.PlayerOne ? _playerOne : _playerTwo;
        }

        #endregion   
    }
}
