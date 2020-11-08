using System;
using Buriola.Board;
using Buriola.Board.Data;
using UnityEngine;
using Buriola.Player;
using Buriola.Interfaces;
using Random = UnityEngine.Random;

namespace Buriola.AI
{
    /// <summary>
    /// This is the base class of every ghost. It implements the IEatable interface to be able to collide with Pacman
    /// and the IGameBoardEvents interface to listen to the board events
    /// </summary>
    public abstract class GhostAI : MonoBehaviour, IEatable, IGameBoardEvents
    {
        #region Variables
        private static bool _caughtPacman;
        private static readonly int GHOST_SCORE_VALUE = 200; 

        protected bool CanMove;
        protected bool IsInGhostHouse; 

        public int PreviousScore { get; private set; }

        //Variables for movement
        private float _currentMoveSpeed;
        private float _normalMoveSpeed;
        private float _scaredMoveSpeed;
        private float _eatenMoveSpeed;

        private float _scaredDuration;
        private float _startBlinkingAt;
        private float _scaredTimer;

        [Header("Board Navigation Settings")]
        [SerializeField]
        protected Node StartingNode = null;
        [SerializeField]
        protected Node HomeNode = null;
        [SerializeField]
        protected Node GhostHouseNode = null;

        protected Node CurrentNode;
        protected Node TargetNode;
        protected Node PreviousNode;

        protected Vector2 Direction;

        private bool _requestStateChange;
        private GhostState _currentGhostState;
        private GhostMode[] _ghostModes;
        private int _modeIndex;
        private float _timer;

        public SpriteRenderer GhostSprite { get; private set; }
        private Animator _anim;
        protected PacmanController Pacman;
        protected GameBoard Board;

        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        private static readonly int NormalTrigger = Animator.StringToHash("NormalTrigger");
        private static readonly int ScaredTrigger = Animator.StringToHash("ScaredTrigger");
        private static readonly int BlinkingTrigger = Animator.StringToHash("BlinkingTrigger");
        private static readonly int DeadTrigger = Animator.StringToHash("DeadTrigger");

        #endregion

        #region UNITY FUNCTIONS

        protected virtual void Start()
        {
            Init();
            SubscribeToBoardEvents();
        }

        protected virtual void Update()
        {
            Movement();
            UpdateGhostsTimers();
            CheckIfItIsAtGhostHouse();
            HandleAnimations();

            //Careful with this -> Got me 2h debugging to find this static variable and reset it on level load :(
            //My fault anyways
            if (_caughtPacman && TargetNode != null)
                TargetNode = null;
        }

        protected void OnDisable()
        {
            UnsubscribeFromBoardEvents();
        }

        #endregion

        /// <summary>
        /// This method initialises the ghost variables and set its default starting position
        /// </summary>
        private void Init()
        {
            //Request a state change
            _requestStateChange = true;

            _currentMoveSpeed = _normalMoveSpeed;
            _currentGhostState = GhostState.Scatter;

            //Init this in case the Level Difficulty object is null
            _ghostModes = new GhostMode[] { new GhostMode(7f, 20f), new GhostMode(7f, 20f), new GhostMode(7f, 20f), new GhostMode(7f, 20f) };

            //Assign references
            _anim = GetComponentInChildren<Animator>();
            Pacman = FindObjectOfType<PacmanController>();
            GhostSprite = GetComponentInChildren<SpriteRenderer>();
            Board = GameBoard.Instance;

            if (StartingNode != null)
            {
                CurrentNode = StartingNode;

                //Set the starting position
                transform.position = StartingNode.transform.localPosition;
            }

            GhostSprite.enabled = false;
        }

        /// <summary>
        /// Updates the animator variables
        /// </summary>
        private void HandleAnimations()
        {
            //Passing the current direction
            _anim.SetFloat(Horizontal, Direction.x);
            _anim.SetFloat(Vertical, Direction.y);
        }

        /// <summary>
        /// Initializes all movement/state variables based on the current level
        /// This method is virtual because every ghost has a couple of special variables that need to be set
        /// This method is subscribing to the OnAfterGameRestart board event
        /// </summary>
        protected virtual void SetGhostsSettings()
        {
            if (Board.CurrentLevelDifficulty != null)
            {
                //Gets the board current level difficulty and updates all variables
                LevelDifficulty difficulty = Board.CurrentLevelDifficulty;

                _normalMoveSpeed = difficulty.GhostsSpeed;
                _scaredMoveSpeed = difficulty.GhostScaredSpeed;
                _eatenMoveSpeed = difficulty.GhostsEatenSpeed;

                _scaredDuration = difficulty.GhostsScareDuration;
                _startBlinkingAt = difficulty.GhostsStartBlinkingAt;
                _ghostModes = difficulty.GhostModes;
            }
        }

        private int CalculateScore()
        {
            // Will return 200, 400, 800 or 1600
            int x = Mathf.RoundToInt((GHOST_SCORE_VALUE * (Mathf.Pow(2, Pacman.KillStreak - 1))));
            PreviousScore = x;
            return x;
        }

        public void OnEaten()
        {
            if (_currentGhostState == GhostState.Chase || _currentGhostState == GhostState.Scatter)
            {
                _caughtPacman = true;
                Pacman.StartDeath();
            }
            else if (_currentGhostState != GhostState.Eaten)
            {
                _requestStateChange = true;
                ChangeState(GhostState.Eaten);

                if (Pacman.KillStreak > 4)
                    Pacman.KillStreak = 1;

                Board.UpdateScore(CalculateScore(), true);
                Pacman.KillStreak++;

                Board.StartGhostEatenEvent(this);
            }
        }

        #region Ghost State Related

        /// <summary>
        /// Changes the ghost state
        /// </summary>
        /// <param name="toState"> The state you want to go </param>
        private void ChangeState(GhostState toState)
        {
            //No point in case this is true
            if (_currentGhostState == toState)
            {
                _requestStateChange = false;
                return;
            }

            // Have to set this variable to true every time you want to change state,
            // because we're using animator triggers, we dont want to call the triggers every frame
            if (_requestStateChange)
            {
                _currentGhostState = toState; //Set our new state

                //Handle animations
                switch (toState)
                {
                    case GhostState.Chase:
                    case GhostState.Scatter:
                        _anim.SetTrigger(NormalTrigger);
                        break;
                    case GhostState.Flee:
                        _anim.SetTrigger(ScaredTrigger);
                        break;
                    case GhostState.Blinking:
                        _anim.SetTrigger(BlinkingTrigger);
                        break;
                    case GhostState.Eaten:
                        _anim.SetTrigger(DeadTrigger);
                        break;
                    default:
                        break;
                }
            }

            //Set this to false to avoid unwanted state changes
            _requestStateChange = false;
        }

        /// <summary>
        /// Updates the timers for each ghost state. Allow us to change between Scatter mode and Chase Mode
        /// Also, changing our current move speed.
        /// Called on Update
        /// </summary>
        private void UpdateGhostsTimers()
        {
            if (_currentGhostState == GhostState.Chase || _currentGhostState == GhostState.Scatter)
                UpdateChaseScatterTimers();
            else if (_currentGhostState == GhostState.Flee || _currentGhostState == GhostState.Blinking)
                UpdateScaredTimers();
            else if (_currentGhostState == GhostState.Eaten)
            {
                _currentMoveSpeed = _eatenMoveSpeed;
            }
        }

        private void UpdateChaseScatterTimers()
        {
            _currentMoveSpeed = _normalMoveSpeed;

            _timer += Time.deltaTime;
            if (_currentGhostState == GhostState.Scatter && _timer > _ghostModes[_modeIndex].scatterModeTime)
            {
                _requestStateChange = true;
                ChangeState(GhostState.Chase);
                _timer = 0;
            }
            
            if (_currentGhostState == GhostState.Chase && _timer > _ghostModes[_modeIndex].chaseModeTime)
            {
                if (_modeIndex != _ghostModes.Length - 1)
                {
                    _modeIndex++;
                    _requestStateChange = true;
                    ChangeState(GhostState.Scatter);
                    _timer = 0;
                }
                else
                    _timer = 0;
            }
        }

        private void UpdateScaredTimers()
        {
            _currentMoveSpeed = _scaredMoveSpeed;

            //Checks if we should go to the Blinking state, let the player know we are going to be dangerous again
            _scaredTimer += Time.deltaTime;
            if (_scaredTimer >= _startBlinkingAt && _currentGhostState != GhostState.Blinking)
            {
                //Change state
                _requestStateChange = true;
                ChangeState(GhostState.Blinking);
            } 
            else if (_scaredTimer > _scaredDuration) //Timer will still be running even on Blinking State
            {
                //Reset Pacman Killstreak
                Pacman.KillStreak = 1;

                //Change state
                _requestStateChange = true;
                ChangeState(GhostState.Chase);
                _scaredTimer = 0f; //reset
            }
        }

        private void StartFleeState()
        {
            if (_currentGhostState == GhostState.Eaten)
                return;

            _scaredTimer = 0f;
            _requestStateChange = true;
            ChangeState(GhostState.Flee);
        }

        #endregion

        #region Ghost Movement Related
        private void Movement()
        {
            if (!CanMove)
                return;

            //Check to see if the destination is not null and if we are not at the ghost house
            if (TargetNode != CurrentNode && TargetNode != null && !IsInGhostHouse)
            {
                //Checks to see if the ghost passed the target node he was headed
                if (PassedNode())
                {
                    //In that case we update the current node 
                    CurrentNode = TargetNode;
                    transform.position = CurrentNode.transform.position; //set our position

                    //Check if we are at a portal node, in that case, will be teleported
                    CheckForPortals();

                    //Choose the next destination based on the ghost current state
                    TargetNode = ChooseNextNode();

                    PreviousNode = CurrentNode; //set the previous location

                    //since the ghost is between nodes, on his way to another, set this to null
                    CurrentNode = null;
                }
                else // if the ghost didnt pass his target node
                {
                    //Move in the current direction
                    transform.localPosition += (Vector3)Direction * (_currentMoveSpeed * Time.deltaTime);
                }
            }
        }

        protected Node ChooseNextNode()
        {
            Vector2 targetTile;

            switch (_currentGhostState)
            {
                case GhostState.Chase:
                    targetTile = FindTargetPosition();
                    break;
                case GhostState.Scatter:
                    targetTile = HomeNode.transform.position;
                    break;
                case GhostState.Flee:
                case GhostState.Blinking:
                    targetTile = FindRandomPosition();
                    break;
                case GhostState.Eaten:
                    targetTile = GhostHouseNode.transform.position;
                    break;
                default:
                    targetTile = Vector2.zero;
                    break;
            }

            Node moveToNode = null;

            Node[] foundNodes = new Node[4];
            Vector2[] foundNodesDirection = new Vector2[4];

            int nodeCounter = 0;

            for (int i = 0; i < CurrentNode.Neighbours.Length; i++)
            {
                if (CurrentNode.ValidDirections[i] != (Vector3)Direction * -1)
                {
                    foundNodes[nodeCounter] = CurrentNode.Neighbours[i];
                    foundNodesDirection[nodeCounter] = CurrentNode.ValidDirections[i];
                    nodeCounter++;
                }
            }

            if (foundNodes.Length == 1)
            {
                moveToNode = foundNodes[0];
                Direction = foundNodesDirection[0];
                return moveToNode;
            }

            if (foundNodes.Length > 1)
            {
                float leastDistance = 100000f;

                for (int i = 0; i < foundNodes.Length; i++)
                {
                    if (foundNodesDirection[i] != Vector2.zero)
                    {
                        float distance = GetDistance(foundNodes[i].transform.position, targetTile);

                        if (distance < leastDistance)
                        {
                            leastDistance = distance;
                            moveToNode = foundNodes[i];
                            Direction = foundNodesDirection[i];
                        }
                    }
                }
            }

            return moveToNode;
        }

        protected abstract Vector2 FindTargetPosition();

        private Vector2 FindRandomPosition()
        {
            int x = Random.Range(0, 28);
            int y = Random.Range(0, 32);

            return new Vector2(x, y);
        }

        private void CheckIfItIsAtGhostHouse()
        {
            if (_currentGhostState != GhostState.Eaten) return;
            
            if (PreviousNode == GhostHouseNode)
            {
                TargetNode = GhostHouseNode.Neighbours[0];

                Direction = Vector2.up;
                    
                _requestStateChange = true;
                ChangeState(GhostState.Chase);

                Board.InvokeGhostReachedHouseEvent();
            }
        }

        #endregion

        #region Board Calculations
        private bool PassedNode()
        {
            float nodeToTarget = LengthFromNode(TargetNode.transform.position);
            float nodeToSelf = LengthFromNode(transform.localPosition);

            return nodeToSelf > nodeToTarget;
        }

        private void CheckForPortals()
        {
            PortalNode otherPortal = Board.GetPortalNodeAtPosition(CurrentNode.transform.position);
            if (otherPortal != null)
            {
                transform.localPosition = otherPortal.gameObject.transform.position;
                CurrentNode = otherPortal;
            }
        }

        private float LengthFromNode(Vector2 targetPosition)
        {
            Vector2 vec = targetPosition - (Vector2)PreviousNode.transform.position;
            return vec.sqrMagnitude;
        }

        protected float GetDistance(Vector2 posA, Vector2 posB)
        {
            float dx = posA.x - posB.x;
            float dy = posA.y - posB.y;

            float distance = Mathf.Sqrt(Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2));

            return distance;
        }

        #endregion

        #region GAME BOARDS EVENTS IMPLEMENTATION

        private void SubscribeToBoardEvents()
        {
            Board.OnGameStart += OnGameStart;
            Board.OnAfterGameStart += OnAfterGameStart;
            Board.OnGameRestart += OnGameBoardRestart;
            Board.OnAfterGameRestart += OnAfterGameBoardRestart;
            Board.OnAfterGameRestart += SetGhostsSettings;

            Board.OnPacmanDied += OnPacmanDied;
            Board.OnSuperPacpointEaten += StartFleeState;
            Board.OnGhostEaten += OnGhostEaten;
            Board.OnAfterGhostEaten += OnAfterGhostEaten;

            Board.OnLevelWin += OnLevelWin;
        }

        private void UnsubscribeFromBoardEvents()
        {
            Board.OnGameStart -= OnGameStart;
            Board.OnAfterGameStart -= OnAfterGameStart;
            Board.OnGameRestart -= OnGameBoardRestart;
            Board.OnAfterGameRestart -= OnAfterGameBoardRestart;
            Board.OnAfterGameRestart -= SetGhostsSettings;

            Board.OnPacmanDied -= OnPacmanDied;
            Board.OnSuperPacpointEaten -= StartFleeState;

            Board.OnGhostEaten -= OnGhostEaten;
            Board.OnAfterGhostEaten -= OnAfterGhostEaten;

            Board.OnLevelWin -= OnLevelWin;
        }

        public void OnGameStart()
        {
            GhostSprite.enabled = true;
            _caughtPacman = false;

            SetGhostsSettings();
        }

        public void OnAfterGameStart()
        {
            CanMove = true;
            TargetNode = ChooseNextNode();
        }

        public virtual void OnGameBoardRestart()
        {
            CanMove = false;
            _requestStateChange = true;
            _caughtPacman = false;
            GhostSprite.enabled = false;

            TargetNode = null;
            CurrentNode = StartingNode;
            PreviousNode = CurrentNode;

            _modeIndex = 0;
            _timer = 0f;
            _scaredTimer = 0f;

            transform.position = StartingNode.transform.localPosition;

            ChangeState(GhostState.Scatter);
        }

        public virtual void OnAfterGameBoardRestart()
        {
            GhostSprite.enabled = true;

            Direction = Vector2.left;
            TargetNode = ChooseNextNode();
            PreviousNode = CurrentNode;

            CanMove = true;
        }

        private void OnPacmanDied()
        {
            GhostSprite.enabled = false;
        }

        public void OnGhostEaten()
        {
            CanMove = false;
        }

        public void OnAfterGhostEaten()
        {
            CanMove = true;
        }

        public void OnLevelWin()
        {
            CanMove = false;
            GhostSprite.enabled = false;
        }
        
        #endregion
    }
}
