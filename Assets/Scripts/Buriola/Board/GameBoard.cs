using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Buriola;
using Pacman.UI;
using Pacman.Controllers;
using Pacman.AI;
using Pacman.Data;

namespace Pacman
{
    /// <summary>
    /// Main controller of the game loop
    /// </summary>
    public class GameBoard : MonoBehaviour
    {
        #region Singleton
        //Singleton instace
        private static GameBoard instance;
        public static GameBoard Instance { get { return instance; } }
        #endregion

        #region Delegates     
        //Game Loop Delegates
        public delegate void OnGameStart();
        public OnGameStart onGameStart;
        public delegate void OnAfterGameStart();
        public OnAfterGameStart onAfterGameStart;
        public delegate void OnGameRestart();
        public OnGameRestart onGameRestart;
        public delegate void OnAfterGameRestart();
        public OnAfterGameRestart onAfterGameRestart;
        public delegate void OnGameOver();
        public OnGameOver onGameOver;
        public delegate void OnAfterGameOver();
        public OnAfterGameOver onAfterGameOver;

        //Item Pickup Delegates
        public delegate void OnGhostEaten();
        public OnGhostEaten onGhostEaten;
        public delegate void OnAfterGhostEaten();
        public OnAfterGhostEaten onAfterGhostEaten;
        public delegate void OnBonusItemEaten(BonusItem fruitEaten);
        public OnBonusItemEaten onBonusItemEaten;
        public delegate void OnSuperPacpointEaten();
        public OnSuperPacpointEaten onSuperPacpointEaten;

        //Important Delegates
        public delegate void OnLevelWin();
        public OnLevelWin onLevelWin;
        public delegate void OnPacmanDied();
        public OnPacmanDied onPacmanDied;

        //Others
        public delegate void OnGhostReachedHouse();
        public OnGhostReachedHouse onGhostReachedHouse;
        #endregion

        #region Variables
        private PacmanController pacman; // Player reference
        private int totalPacpoints; // the total pacpoints on the board

        //Game State 
        public enum GameState { NewGame, InGame, Restart, GameOver }
        public GameState gameState;

        //Board bounds and Node References to match each position of the board
        private static int boardWidth = 28;
        private static int boardHeight = 36;
        public Node[,] board = new Node[boardWidth, boardHeight];

        //Aux Bools
        private bool processWinLevel; //Flag to go to the next level
        private bool multiplayer; //Flag to trigger multiplayer
        private bool showBonusItem1; //Show first bonus item
        private bool showBonusItem2; //Show second bonus item

        //Player Variables - Used to switch between players
        public enum Players { PlayerOne = 0, PlayerTwo = 1 }
        public Players currentPlayer; //Who is playing the level
        private PlayerStats playerOne; // Player One Stats (Current Level, Lives and etc)
        private PlayerStats playerTwo; // Player Two Stats (Current Level, Lives and etc)
        //Used to save the board state for each player when on Multiplayer mode
        private bool[] playerOneBoardState;
        private bool[] playerTwoBoardState;

        //The current level difficulty
        [HideInInspector]
        public LevelDifficulty currentLevelDifficulty;
        //The other levels - Key (The Level), Value (Variables)
        private Dictionary<int, LevelDifficulty> levelDifficulties = new Dictionary<int, LevelDifficulty>();

        //Audio clip references
        [Header("Audio Settings")]
        private AudioSource audioS;
        [SerializeField]
        private AudioClip backgroundSound = null;
        [SerializeField]
        private AudioClip ghostsFrightnedSound = null;
        [SerializeField]
        private AudioClip ghostsRetreatSound = null;
        [SerializeField]
        private AudioClip ghostEatenSound = null;
        [SerializeField]
        private AudioClip bonusItemEaten = null;
        [SerializeField]
        private AudioClip winLevelClip = null;

        //The available bonus Items
        [Header("Bonus Items")]
        [SerializeField]
        private GameObject[] bonusItems = new GameObject[8];

        //UI reference
        [Header("UI")]
        [SerializeField]
        private GameUI gameUI = null;
        #endregion

        #region UNITY FUNCTIONS
        private void Awake()
        {
            if (instance != null && instance != this)
                Destroy(gameObject);

            //Singleton assigned
            instance = this;
        }

        private void Start()
        {
            Init();
            StartNewGame();
        }

        private void Update()
        {
            if (WinLevel(currentPlayer) && !processWinLevel)
            {
                processWinLevel = true;
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

        /// <summary>
        /// Method to initiate the board
        /// Called on Start
        /// </summary>
        private void Init()
        {
            showBonusItem1 = false;
            showBonusItem2 = false;

            //Get references
            pacman = FindObjectOfType<PacmanController>();
            audioS = GetComponent<AudioSource>();

            gameState = GameState.NewGame;
            currentPlayer = Players.PlayerOne;

            //Init all data
            FindAllPacpoints();
            FindAllNodes();
            LoadLevelsData();
            //Subscribe to events
            SubscribeToDelegates();

            //Set multiplayer mode
            if (GameController.Instance != null)
                multiplayer = !GameController.isOnePlayerGame;

            //Init player stats
            if (multiplayer)
            {
                playerOne = new PlayerStats(1, 3, 0);
                playerTwo = new PlayerStats(1, 3, 0);
            }
            else
                playerOne = new PlayerStats(1, 3, 0);

            //Show the current player
            gameUI.ShowPlayerText(currentPlayer, 1.5f);
        }

        #region GAME LOOP
        /// <summary>
        /// Triggers the New Game Coroutine
        /// </summary>
        private void StartNewGame()
        {
            StartCoroutine(NewGame());
        }
        /// <summary>
        /// Starts a new game
        /// </summary>
        /// <returns></returns>
        private IEnumerator NewGame()
        {
            yield return new WaitForSeconds(2.25f);
            //Sets bonus items UI
            gameUI.SetFruitImage(GetPlayerStats(currentPlayer).CurrentLevel);

            //Set level difficulty
            ChangeLevelDifficulty(GetPlayerStats(currentPlayer));

            //Call event
            if (onGameStart != null)
                onGameStart.Invoke();

            //Show ready text
            gameUI.ShowReadyText(1.25f);

            //If it is a multiplayer game, we save the board state for each player
            if (multiplayer)
            {
                SaveBoardState(Players.PlayerOne);
                SaveBoardState(Players.PlayerTwo);
            }

            yield return new WaitForSeconds(2f);

            //Call last start event
            if (onAfterGameStart != null)
                onAfterGameStart.Invoke();

            //Set game state
            gameState = GameState.InGame;

            //Play normal background sound
            PlayAudioClip(backgroundSound);
        }

        /// <summary>
        /// Triggers the Restart Coroutine and checks for Game Over
        /// </summary>
        public void StartRestart()
        {
            //Update game state
            gameState = GameState.Restart;

            //Current player loses a life
            GetPlayerStats(currentPlayer).LoseLife();
            SaveBoardState(currentPlayer); // saves his board

            //Update UI lives
            gameUI.UpdatePlayerLives(GetPlayerStats(currentPlayer).GetLives());

            //reset this
            showBonusItem1 = false;
            showBonusItem2 = false;

            //Check if the current player has zero lives
            if (GetPlayerStats(currentPlayer).GameOver())
            {
                //Then we trigger a Game Over for this player
                gameState = GameState.GameOver;
                StartGameOver();
            }
            else
            {
                //If it is a multiplayer game
                if (multiplayer)
                {
                    //Change the current player
                    SwapCurrentPlayer();

                    //Get difficulty for the new player
                    ChangeLevelDifficulty(GetPlayerStats(currentPlayer));
                    LoadBoardState(currentPlayer); //Load his board state

                    //Update UI current player
                    gameUI.SwapPlayers(currentPlayer);
                    //Then restart
                }

                //If it isn't a multiplayer game
                //Just reset variables and trigger Restart
                ChangeLevelDifficulty(GetPlayerStats(currentPlayer));
                StartCoroutine(Restart());
            }
        }
        /// <summary>
        /// Restarts the game
        /// </summary>
        /// <returns></returns>
        private IEnumerator Restart()
        {
            //Update UI / Show who the current player is and show ready text
            gameUI.SetFruitImage(GetPlayerStats(currentPlayer).CurrentLevel);
            gameUI.ShowReadyText(1.25f);
            gameUI.ShowPlayerText(currentPlayer, 1.25f);

            //Call board event
            if (onGameRestart != null)
                onGameRestart.Invoke();

            yield return new WaitForSeconds(1.25f);

            //Call board event
            if (onAfterGameRestart != null)
                onAfterGameRestart.Invoke();

            //Update game state
            gameState = GameState.InGame;
            //Play music
            PlayAudioClip(backgroundSound);
        }

        /// <summary>
        /// Triggers the Game Over couroutine
        /// </summary>
        private void StartGameOver()
        {
            gameState = GameState.GameOver;
            StartCoroutine(GameOver());
        }
        /// <summary>
        /// Handles a Game Over state for the current player
        /// </summary>
        /// <returns></returns>
        private IEnumerator GameOver()
        {
            //Show Game over text
            gameUI.ShowGameOverText(2f);
            yield return new WaitForSeconds(3f);

            //If its a multiplayer game
            if (multiplayer)
            {
                //Check if it is Game Over for the other player as well
                Players otherPlayer = currentPlayer == Players.PlayerOne ? Players.PlayerTwo : Players.PlayerOne;
                if (GetPlayerStats(otherPlayer).GameOver())
                {
                    //If it is
                    if (GameController.Instance != null)
                    {
                        //Save high score
                        GameController.Instance.SaveHighScore(GetPlayerStats(currentPlayer).Score);
                        GameController.Instance.SaveHighScore(GetPlayerStats(otherPlayer).Score);

                        yield return new WaitForEndOfFrame();

                        //Go back to the Menu Scene
                        GameController.isOnePlayerGame = true;
                        GameController.Instance.RequestSceneChange(1);
                    }
                }
                else // if it isnt, there is still a player with lives
                {
                    //Save the high score for the player who lost
                    GameController.Instance.SaveHighScore(GetPlayerStats(currentPlayer).Score);

                    // Then we trigger a restart
                    gameState = GameState.Restart;

                    // Swap and setup UI
                    SwapCurrentPlayer();
                    gameUI.SetupUI();
                    gameUI.SwapPlayers(currentPlayer);

                    //Load his board state
                    LoadBoardState(currentPlayer);
                    StartCoroutine(Restart()); // Restart
                    yield break;
                }
            }
            else // if it is not a multiplayer game
            {
                if (GameController.Instance != null)
                {
                    //Save high score
                    GameController.Instance.SaveHighScore(GetPlayerStats(currentPlayer).Score);

                    yield return new WaitForEndOfFrame();

                    //Go back to Main Menu
                    GameController.isOnePlayerGame = true;
                    GameController.Instance.RequestSceneChange(1);
                }
            }
        }

        /// <summary>
        /// Checks to see if the current player has beat the level
        /// Called on Update
        /// </summary>
        /// <param name="currentPlayer">Who is playing</param>
        /// <returns>True- won level, False - not won</returns>
        private bool WinLevel(Players currentPlayer)
        {
            //If he ate all the pacpoints of the level
            return GetPlayerStats(currentPlayer).PacpointsConsumed >= totalPacpoints;
        }
        /// <summary>
        /// Triggers a win level state and prepare for next level
        /// </summary>
        /// <returns></returns>
        private IEnumerator Win()
        {
            //Stop all music
            audioS.Stop();

            //Dramatic pause for a 1s
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(1f);
            Time.timeScale = 1;

            //Call event
            if (onLevelWin != null)
                onLevelWin.Invoke();

            yield return new WaitForSeconds(1f);

            //Reset variables
            showBonusItem1 = false;
            showBonusItem2 = false;

            //Increments the current player level and resets his pacpoints
            GetPlayerStats(currentPlayer).CurrentLevel++;
            GetPlayerStats(currentPlayer).BonusItemShown = false;
            GetPlayerStats(currentPlayer).PacpointsConsumed = 0;

            //Adjust level difficulty
            ChangeLevelDifficulty(GetPlayerStats(currentPlayer));

            //Play win music
            audioS.PlayOneShot(winLevelClip);
            //Show current Level UI
            gameUI.ShowLevelText(GetPlayerStats(currentPlayer).CurrentLevel, 5f);

            yield return new WaitForSeconds(5f);

            //Activates all pacpoints
            Transform pacpointParent = transform.GetChild(1);
            for (int i = 0; i < pacpointParent.childCount; i++)
            {
                pacpointParent.GetChild(i).gameObject.SetActive(true);
            }

            //Activates all super pacpoints
            Transform superPacpointParent = transform.GetChild(2);
            for (int i = 0; i < superPacpointParent.childCount; i++)
            {
                superPacpointParent.GetChild(i).gameObject.SetActive(true);
            }

            //Save the board state
            SaveBoardState(currentPlayer);

            //Trigger restart
            gameState = GameState.Restart;
            StartCoroutine(Restart());

            //Reset flag
            processWinLevel = false;
        }

        /// <summary>
        /// Swaps the current player
        /// </summary>
        private void SwapCurrentPlayer()
        {
            currentPlayer = (currentPlayer == Players.PlayerOne) ? Players.PlayerTwo : Players.PlayerOne;
            //Update UI
            gameUI.UpdatePlayerLives(GetPlayerStats(currentPlayer).GetLives());
        }

        /// <summary>
        /// Updates the score for the current player
        /// </summary>
        /// <param name="value">The value to be added</param>
        /// <param name="isGhost">If it is a ghost</param>
        /// <param name="isBonusItem">If it is a bonus item</param>
        public void UpdateScore(int value, bool isGhost = false, bool isBonusItem = false)
        {
            //If none of these options, increment the numbar of pacpoints eaten
            if (!isGhost && !isBonusItem)
                GetPlayerStats(currentPlayer).PacpointsConsumed++;

            //Add score
            GetPlayerStats(currentPlayer).Score += value;

            //Update UI score text for the current player
            gameUI.UpdatePlayerScore(currentPlayer, GetPlayerStats(currentPlayer).Score);
        }

        #endregion

        #region Board Data & Calculations
        /// <summary>
        /// This method find all Nodes in the scene and populates the board matrix with the positions
        /// </summary>
        private void FindAllNodes()
        {
            Node[] objects = FindObjectsOfType<Node>();

            foreach (Node n in objects)
            {
                Vector2 pos = n.transform.position;

                float x_index = Mathf.Abs(pos.x);
                float y_index = Mathf.Abs(pos.y);

                board[(int)x_index, (int)y_index] = n;
            }
        }

        /// <summary>
        /// Counts how many pacpoints exist in the scene and add 4 to include the superpacpoints
        /// </summary>
        private void FindAllPacpoints()
        {
            Transform t = transform.GetChild(1);
            totalPacpoints = t.childCount + 4; //Include Superpacpoints
        }

        /// <summary>
        /// Saves the board state for the current player
        /// </summary>
        /// <param name="currentPlayer">The current player to save</param>
        private void SaveBoardState(Players currentPlayer)
        {
            Transform pacpointsParent = transform.GetChild(1);
            Transform superPacpointsParent = transform.GetChild(2);

            //Populate the bool arrays with Pacpoint and Superpacpoint active State in scene
            if (currentPlayer == Players.PlayerOne)
            {
                int lastIndex = 0;
                playerOneBoardState = new bool[pacpointsParent.childCount + superPacpointsParent.childCount];
                for (int i = 0; i < pacpointsParent.childCount; i++)
                {
                    playerOneBoardState[i] = pacpointsParent.GetChild(i).gameObject.activeSelf;
                    lastIndex = i;
                }

                for (int i = 0; i < superPacpointsParent.childCount; i++)
                {
                    playerOneBoardState[lastIndex + i + 1] = superPacpointsParent.GetChild(i).gameObject.activeSelf;
                }
            }
            else
            {
                int lastIndex = 0;
                playerTwoBoardState = new bool[pacpointsParent.childCount + superPacpointsParent.childCount];
                for (int i = 0; i < pacpointsParent.childCount; i++)
                {
                    playerTwoBoardState[i] = pacpointsParent.GetChild(i).gameObject.activeSelf;
                    lastIndex = i;
                }

                for (int i = 0; i < superPacpointsParent.childCount; i++)
                {
                    playerTwoBoardState[lastIndex + i + 1] = superPacpointsParent.GetChild(i).gameObject.activeSelf;
                }
            }
        }

        /// <summary>
        /// Loads the board state for the current player
        /// </summary>
        /// <param name="currentPlayer">The current player to load</param>
        private void LoadBoardState(Players currentPlayer)
        {
            if (playerOneBoardState == null && playerTwoBoardState == null)
                return;

            if (playerOneBoardState.Length == 0 || playerTwoBoardState.Length == 0)
                return;

            Transform pacpointsParent = transform.GetChild(1);
            Transform superPacpointsParent = transform.GetChild(2);

            //Checks the bool array and set the Active state for each Pacpoint and super pacpoint in the scene
            if (currentPlayer == Players.PlayerOne)
            {
                int lastIndex = 0;
                for (int i = 0; i < pacpointsParent.childCount; i++)
                {
                    pacpointsParent.GetChild(i).gameObject.SetActive(playerOneBoardState[i]);
                    lastIndex = i;
                }

                for (int i = 0; i < superPacpointsParent.childCount; i++)
                {
                    superPacpointsParent.GetChild(i).gameObject.SetActive(playerOneBoardState[lastIndex + i + 1]);
                }
            }
            else
            {
                int lastIndex = 0;
                for (int i = 0; i < pacpointsParent.childCount; i++)
                {
                    pacpointsParent.GetChild(i).gameObject.SetActive(playerTwoBoardState[i]);
                    lastIndex = i;
                }

                for (int i = 0; i < superPacpointsParent.childCount; i++)
                {
                    superPacpointsParent.GetChild(i).gameObject.SetActive(playerTwoBoardState[lastIndex + i + 1]);
                }
            }
        }

        /// <summary>
        /// Loads from the Resources folder all the Level Difficulties scriptables
        /// </summary>
        private void LoadLevelsData()
        {
            LevelDifficulty[] levelDifficulties = Resources.LoadAll<LevelDifficulty>("Data");
            if (levelDifficulties.Length > 0)
            {
                for (int i = 0; i < levelDifficulties.Length; i++)
                {
                    //Add to the dictionary
                    this.levelDifficulties.Add(i + 1, levelDifficulties[i]);
                }
            }
        }

        /// <summary>
        /// Change the current level difficulty based on the current player level.
        /// </summary>
        /// <param name="playerStats">The player stats</param>
        private void ChangeLevelDifficulty(PlayerStats playerStats)
        {
            //Set this to be the last in case the player passes 8 levels
            if (playerStats.CurrentLevel > levelDifficulties.Count)
                currentLevelDifficulty = levelDifficulties.Values.Last();
            else
                currentLevelDifficulty = levelDifficulties[playerStats.CurrentLevel];
        }

        /// <summary>
        /// Gets the nearest Portal Node based on a given position
        /// </summary>
        /// <param name="pos">The position you want to check</param>
        /// <returns>The opposite portal node</returns>
        public PortalNode GetPortalNodeAtPosition(Vector2 pos)
        {
            float x_index = Mathf.Abs(pos.x);
            float y_index = Mathf.Abs(pos.y);

            //Get from the board array
            PortalNode node = board[(int)(x_index), (int)y_index] as PortalNode;

            if (node != null)
            {
                if (node.portalReceiver != null)
                    //Returns the other portal
                    return node.portalReceiver;
            }

            return null;
        }

        #endregion

        #region Board Events
        /// <summary>
        /// Triggers the ghost eaten event coroutine
        /// </summary>
        /// <param name="ghostEaten">The ghost that was eaten</param>
        public void StartGhostEatenEvent(GhostAI ghostEaten)
        {
            StartCoroutine(GhostEaten(ghostEaten));
        }
        /// <summary>
        /// Handles ghost eaten event
        /// </summary>
        /// <param name="ghostEaten"> the ghost eaten</param>
        /// <returns></returns>
        private IEnumerator GhostEaten(GhostAI ghostEaten)
        {
            //Call board event
            if (onGhostEaten != null)
                onGhostEaten.Invoke();

            //Stop all music and disables pacman and the ghost sprite
            audioS.Stop();
            ghostEaten.ghostSprite.enabled = false;
            pacman.pacmanSprite.enabled = false;

            //Show the score
            gameUI.SetGhostEatenScoreText(ghostEaten.transform.position, ghostEaten.previousScore);

            //Play sound eaten
            audioS.PlayOneShot(ghostEatenSound);

            yield return new WaitForSeconds(1f);

            //Enables sprites again
            pacman.pacmanSprite.enabled = true;
            ghostEaten.ghostSprite.enabled = true;

            //Call board event
            if (onAfterGhostEaten != null)
                onAfterGhostEaten.Invoke();

            //Play retreat sound 
            PlayAudioClip(ghostsRetreatSound);
        }

        /// <summary>
        /// Trigger bonus item eaten coroutine
        /// </summary>
        /// <param name="item">The bonus item eaten</param>
        private void StartBonusItemEaten(BonusItem item)
        {
            StartCoroutine(BonusItemEaten(item));
        }
        /// <summary>
        /// Handles the bonus item eaten event
        /// </summary>
        /// <param name="item">the bonus item eaten</param>
        /// <returns></returns>
        private IEnumerator BonusItemEaten(BonusItem item)
        {
            //Stops all music
            audioS.Stop();
            //Show UI score
            gameUI.SetGhostEatenScoreText(item.transform.localPosition, item.ScoreValue);

            //Play item sound
            audioS.PlayOneShot(bonusItemEaten);

            yield return new WaitForEndOfFrame();

            //play normal sound again
            PlayAudioClip(backgroundSound);
        }

        /// <summary>
        /// Checks if should show a bonus item based on how many pacpoints Pacman ate
        /// Called on Update
        /// </summary>
        private void ShowBonusItem()
        {
            //If we already showed the bonus item twice, no point in showing again for this player
            if (GetPlayerStats(currentPlayer).BonusItemShown)
                return;

            //Show first item after 70 pacpoints consumed
            if (!showBonusItem1 && GetPlayerStats(currentPlayer).PacpointsConsumed >= 70)
            {
                for (int i = 0; i < bonusItems.Length; i++)
                {
                    if (bonusItems[i].name == currentLevelDifficulty.bonusItem.name)
                    {
                        bonusItems[i].SetActive(true);
                        showBonusItem1 = true;
                        break;
                    }
                }
            }

            //Show second item after 170 pacpoint consumed
            if (!showBonusItem2 && GetPlayerStats(currentPlayer).PacpointsConsumed >= 170)
            {
                for (int i = 0; i < bonusItems.Length; i++)
                {
                    if (bonusItems[i].name == currentLevelDifficulty.bonusItem.name)
                    {
                        bonusItems[i].SetActive(true);
                        showBonusItem2 = true;
                        //Set this flag to not show items again on this level
                        GetPlayerStats(currentPlayer).BonusItemShown = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Subscribe to board delegates
        /// Called on Init
        /// </summary>
        private void SubscribeToDelegates()
        {
            onPacmanDied += StopSounds;
            onSuperPacpointEaten += delegate { PlayAudioClip(ghostsFrightnedSound); };
            onGhostReachedHouse += delegate { PlayAudioClip(backgroundSound); };
            onBonusItemEaten += StartBonusItemEaten;
        }

        /// <summary>
        /// Unsubscribe from board delegates
        /// Called on OnDisable
        /// </summary>
        private void UnsubscribeFromDelegates()
        {
            onPacmanDied -= StopSounds;
            onSuperPacpointEaten -= delegate { PlayAudioClip(ghostsFrightnedSound); };
            onGhostReachedHouse -= delegate { PlayAudioClip(backgroundSound); };
            onBonusItemEaten -= StartBonusItemEaten;
        }
        #endregion

        #region Audio Related
        //Play sounds, nothing out of the ordinary here
        private void PlayAudioClip(AudioClip clip)
        {
            audioS.clip = clip;
            audioS.loop = true;
            audioS.Play();
        }

        private void StopSounds()
        {
            audioS.Stop();
        }

        #endregion

        #region Getters

        /// <summary>
        /// Gets the player stats
        /// </summary>
        /// <param name="player">The player you want to get</param>
        /// <returns>The player stats</returns>
        public PlayerStats GetPlayerStats(Players player)
        {
            return player == Players.PlayerOne ? playerOne : playerTwo;
        }

        #endregion   
    }
}
