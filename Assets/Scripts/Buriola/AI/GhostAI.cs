using Buriola.Board;
using Buriola.Board.Data;
using UnityEngine;
using Buriola.Player;
using Buriola.Interfaces;

namespace Buriola.AI
{
    /// <summary>
    /// This is the base class of every ghost. It implements the IEatable interface to be able to collide with Pacman
    /// and the IGameBoardEvents interface to listen to the board events
    /// </summary>
    public abstract class GhostAI : MonoBehaviour, IEatable, IGameBoardEvents
    {
        #region Variables
        protected static bool caughtPacman; //Shared variable to let every ghost know if the caught pacman

        protected bool canMove; //Flag to allow ghosts to move
        protected bool isInGhostHouse; // Flag to add a delay if this ghost is supposed to be in the ghost house

        protected int ghostScoreValue = 200; // the default score value when the ghost is consumed
        [HideInInspector]
        public int previousScore; //aux value to update interface. Stores the last ghost score

        //Variables for movement
        protected float currentMoveSpeed;
        protected float normalMoveSpeed = 6f;
        protected float scaredMoveSpeed = 4f;
        protected float eatenMoveSpeed = 10f;

        protected float scaredDuration = 10f; //How long the ghost is supposed to be frightened
        protected float startBlinkingAt = 7f; //Will start blinking at this value
        protected float scaredTimer;

        [Header("Board Navigation Settings")]
        [SerializeField]
        protected Node startingNode; //The starting position on the board
        [SerializeField]
        protected Node homeNode; //Reference to the home node of this ghost, on Scatter mode, he will be headed this way
        [SerializeField]
        protected Node ghostHouseNode; //Reference to the ghost house node. So he can respawn after being consumed

        protected Node currentNode; //His current board position
        protected Node targetNode; //The position it is headed
        protected Node previousNode; // the previous position

        //Direction references
        protected Vector2 direction; 
        protected Vector2 nextDirection;

        //Aux bool to trigger state change
        protected bool requestStateChange;

        //Ghosts states
        protected enum GhostState
        {
            Chase, Scatter, Flee, Blinking, Eaten
        }
        protected GhostState currentGhostState;
        protected GhostState previousGhostState;

        /// <summary>
        /// The current Ghost Mode
        /// </summary>
        protected GhostMode[] ghostModes;
        protected int modeIndex; //Index of this array
        protected float timer = 0f; //aux timer to change modes

        //Other references
        [HideInInspector]
        public SpriteRenderer ghostSprite;
        protected Animator anim;
        protected PacmanController pacman; //Need a reference of the player
        protected GameBoard board;

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
            if (caughtPacman && targetNode != null)
                targetNode = null;
        }

        protected void OnDisable()
        {
            UnsubscribeFromBoardEvents();
        }

        #endregion

        /// <summary>
        /// This method initialises the ghost variables and set its default starting position
        /// </summary>
        protected void Init()
        {
            //Request a state change
            requestStateChange = true;

            currentMoveSpeed = normalMoveSpeed;
            currentGhostState = GhostState.Scatter;

            //Init this in case the Level Difficulty object is null
            ghostModes = new GhostMode[] { new GhostMode(7f, 20f), new GhostMode(7f, 20f), new GhostMode(7f, 20f), new GhostMode(7f, 20f) };

            //Assign references
            anim = GetComponentInChildren<Animator>();
            pacman = FindObjectOfType<PacmanController>();
            ghostSprite = GetComponentInChildren<SpriteRenderer>();
            board = GameBoard.Instance;

            if (startingNode != null)
            {
                currentNode = startingNode;

                //Set the starting position
                transform.position = startingNode.transform.localPosition;
            }

            ghostSprite.enabled = false;
        }

        /// <summary>
        /// Updates the animator variables
        /// </summary>
        protected void HandleAnimations()
        {
            //Passing the current direction
            anim.SetFloat("Horizontal", direction.x);
            anim.SetFloat("Vertical", direction.y);
        }

        /// <summary>
        /// Initializes all movement/state variables based on the current level
        /// This method is virtual because every ghost has a couple of special variables that need to be set
        /// This method is subscribing to the OnAfterGameRestart board event
        /// </summary>
        protected virtual void SetGhostsSettings()
        {
            if (board.currentLevelDifficulty != null)
            {
                //Gets the board current level difficulty and updates all variables
                LevelDifficulty difficulty = board.currentLevelDifficulty;

                normalMoveSpeed = difficulty.ghostsSpeed;
                scaredMoveSpeed = difficulty.ghostScaredSpeed;
                eatenMoveSpeed = difficulty.ghostsEatenSpeed;

                scaredDuration = difficulty.ghostsScareDuration;
                startBlinkingAt = difficulty.ghostsStartBlinkingAt;
                ghostModes = difficulty.ghostModes;
            }
        }

        /// <summary>
        /// Calculates the score that Pacman will earn when eating this ghost.
        /// It uses the KillStreak property of Pacman to calculate
        /// </summary>
        /// <returns>The new score value</returns>
        public int CalculateScore()
        {
            // Will return 200, 400, 800 or 1600
            int x = Mathf.RoundToInt((ghostScoreValue * (Mathf.Pow(2, pacman.KillStreak - 1))));
            previousScore = x; //Save this to update UI
            return x;
        }

        /// <summary>
        /// Implementation of the IEatable interface. Allow collision with Pacman
        /// </summary>
        public void OnEaten()
        {
            //If the ghost isn't on Frightned/Flee State
            if (currentGhostState == GhostState.Chase || currentGhostState == GhostState.Scatter)
            {
                //That means its game over for Pacman
                caughtPacman = true;
                pacman.StartDeath();
            }
            else if (currentGhostState != GhostState.Eaten) //If we are not on Consumed/Eaten state, we are at Flee state
            {
                //Change our state
                requestStateChange = true;
                ChangeState(GhostState.Eaten);

                //Not allow Pacman to score more than 1600 points when eating this ghost
                if (pacman.KillStreak > 4)
                    pacman.KillStreak = 1;

                //Updates the player score
                board.UpdateScore(CalculateScore(), true);
                pacman.KillStreak++; //Increments this to allow Pacman to eat more ghosts and score more

                //Trigger the board event
                board.StartGhostEatenEvent(this);
            }
        }

        #region Ghost State Related

        /// <summary>
        /// Changes the ghost state
        /// </summary>
        /// <param name="toState"> The state you want to go </param>
        protected void ChangeState(GhostState toState)
        {
            //No point in case this is true
            if (currentGhostState == toState)
            {
                requestStateChange = false;
                return;
            }

            // Have to set this variable to true every time you want to change state,
            // because we're using animator triggers, we dont want to call the triggers every frame
            if (requestStateChange)
            {
                previousGhostState = currentGhostState; //Save our previous state
                currentGhostState = toState; //Set our new state

                //Handle animations
                switch (toState)
                {
                    case GhostState.Chase:
                    case GhostState.Scatter:
                        anim.SetTrigger("NormalTrigger");
                        break;
                    case GhostState.Flee:
                        anim.SetTrigger("ScaredTrigger");
                        break;
                    case GhostState.Blinking:
                        anim.SetTrigger("BlinkingTrigger");
                        break;
                    case GhostState.Eaten:
                        anim.SetTrigger("DeadTrigger");
                        break;
                    default:
                        break;
                }
            }

            //Set this to false to avoid unwanted state changes
            requestStateChange = false;
        }

        /// <summary>
        /// Updates the timers for each ghost state. Allow us to change between Scatter mode and Chase Mode
        /// Also, changing our current move speed.
        /// Called on Update
        /// </summary>
        protected void UpdateGhostsTimers()
        {
            if (currentGhostState == GhostState.Chase || currentGhostState == GhostState.Scatter)
                UpdateChaseScatterTimers();
            else if (currentGhostState == GhostState.Flee || currentGhostState == GhostState.Blinking)
                UpdateScaredTimers();
            else if (currentGhostState == GhostState.Eaten)
            {
                if (currentMoveSpeed != eatenMoveSpeed)
                    currentMoveSpeed = eatenMoveSpeed;
            }
        }

        /// <summary>
        /// Updates the Chase Mode timer
        /// Called on UpdateGhostsTimers
        /// </summary>
        protected void UpdateChaseScatterTimers()
        {
            //Set our normal speed in case it isnt
            if (currentMoveSpeed != normalMoveSpeed)
                currentMoveSpeed = normalMoveSpeed;

            //The modeIndex allow us to check the current ghost mode and how long we should increment this
            timer += Time.deltaTime;
            if (currentGhostState == GhostState.Scatter && timer > ghostModes[modeIndex].scatterModeTime)
            {
                //Change the state and reset timer
                requestStateChange = true;
                ChangeState(GhostState.Chase);
                timer = 0;
            }
            if (currentGhostState == GhostState.Chase && timer > ghostModes[modeIndex].chaseModeTime)
            {
                //If we didn't reach the end of the array continue to change mode
                if (modeIndex != ghostModes.Length - 1)
                {
                    modeIndex++;
                    requestStateChange = true;
                    ChangeState(GhostState.Scatter);
                    timer = 0;
                }
                else // otherwise, we are at Chase Mode till the end of the round
                    timer = 0;
            }
        }

        /// <summary>
        /// Updates the Frightened/Flee/Scare timer
        /// Called on UpdateGhostsTimers
        /// </summary>
        protected void UpdateScaredTimers()
        {
            //Set the scared move speed if it isnt already
            if (currentMoveSpeed != scaredMoveSpeed)
                currentMoveSpeed = scaredMoveSpeed;

            //Checks if we should go to the Blinking state, let the player know we are going to be dangerous again
            scaredTimer += Time.deltaTime;
            if (scaredTimer >= startBlinkingAt && currentGhostState != GhostState.Blinking)
            {
                //Change state
                requestStateChange = true;
                ChangeState(GhostState.Blinking);
            } 
            else if (scaredTimer > scaredDuration) //Timer will still be running even on Blinking State
            {
                //Reset Pacman Killstreak
                pacman.KillStreak = 1;

                //Change state
                requestStateChange = true;
                ChangeState(GhostState.Chase);
                scaredTimer = 0f; //reset
            }
        }

        /// <summary>
        /// This method triggers the Frightened/Scared/Flee state
        /// Subscribes to the OnSuperPacpointEaten board event
        /// </summary>
        protected void StartFleeState()
        {
            //If we are consumed and still returning to the ghost house, we dont want to change back to Scared state
            if (currentGhostState == GhostState.Eaten)
                return;

            //Reset and change state
            scaredTimer = 0f;
            requestStateChange = true;
            ChangeState(GhostState.Flee);
        }

        #endregion

        #region Ghost Movement Related
        /// <summary>
        /// Handles ghost movement from board node to node
        /// Called on Update
        /// </summary>
        protected void Movement()
        {
            if (!canMove)
                return;

            //Check to see if the destination is not null and if we are not at the ghost house
            if (targetNode != currentNode && targetNode != null && !isInGhostHouse)
            {
                //Checks to see if the ghost passed the target node he was headed
                if (PassedNode())
                {
                    //In that case we update the current node 
                    currentNode = targetNode;
                    transform.position = currentNode.transform.position; //set our position

                    //Check if we are at a portal node, in that case, will be teleported
                    CheckForPortals();

                    //Choose the next destination based on the ghost current state
                    targetNode = ChooseNextNode();

                    previousNode = currentNode; //set the previous location

                    //since the ghost is between nodes, on his way to another, set this to null
                    currentNode = null;
                }
                else // if the ghost didnt pass his target node
                {
                    //Move in the current direction
                    transform.localPosition += (Vector3)direction * currentMoveSpeed * Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// This method will choose the next target node for the ghost
        /// Called on Movement
        /// </summary>
        /// <returns> The next target node, next destination </returns>
        protected Node ChooseNextNode()
        {
            //The position we are headed
            Vector2 targetTile = Vector2.zero;

            //Switch between the States to find the next position
            if (currentGhostState == GhostState.Chase)
                targetTile = FindTargetPosition(); //Look for a position while Chasing Pacman
            else if (currentGhostState == GhostState.Scatter)
                targetTile = homeNode.transform.position; //Go to the home node if we are Scattering
            else if (currentGhostState == GhostState.Flee || currentGhostState == GhostState.Blinking)
                targetTile = FindRandomPosition(); //Choose a Random position if we are Scared/Frightened
            else if (currentGhostState == GhostState.Eaten)
                targetTile = ghostHouseNode.transform.position; //Go to the ghost house if we were eaten by Pacman

            //The node we should go
            Node moveToNode = null;

            //Found nodes and directions based on the current position
            Node[] foundNodes = new Node[4];
            Vector2[] foundNodesDirection = new Vector2[4];

            //How many nodes we found
            int nodeCounter = 0;

            //Check all node neighbours of the current node we are at
            for (int i = 0; i < currentNode.neighbours.Length; i++)
            {
                //Ghosts are not allowed to switch direction
                //Example: if we going Right, cant change to Left in the middle of the process
                //Check the valid directions of that node and verifies if it isnt a switchback
                if (currentNode.validDirections[i] != (Vector3)direction * -1)
                {
                    //Then it found a valid node and direction to go
                    foundNodes[nodeCounter] = currentNode.neighbours[i];
                    foundNodesDirection[nodeCounter] = currentNode.validDirections[i];
                    nodeCounter++;
                }
            }

            //Only one node found
            if (foundNodes.Length == 1)
            {
                //Set our next target node and direction
                moveToNode = foundNodes[0];
                direction = foundNodesDirection[0];
                return moveToNode; // returns the node
            }

            //Otherwise, we have to calculate the shortest distance between all the nodes found
            if (foundNodes.Length > 1)
            {
                //Default value just in case
                float leastDistance = 100000f;

                //Loop through all found nodes until we find the best option
                for (int i = 0; i < foundNodes.Length; i++)
                {
                    if (foundNodesDirection[i] != Vector2.zero)
                    {
                        //Calculate the distance from the position of the node to the target position we want to go
                        //A straight line
                        float distance = GetDistance(foundNodes[i].transform.position, targetTile);

                        //If it is smaller
                        if (distance < leastDistance)
                        {
                            // set the distance
                            leastDistance = distance;
                            moveToNode = foundNodes[i]; // the next target node
                            direction = foundNodesDirection[i]; // the next direction
                        }
                    }
                }
            }

            return moveToNode; // returns the target node
        }

        /// <summary>
        /// Since every ghost has a different way to find the player on the board, this method will be implemented
        /// in the child classes with every ghost specification.
        /// Called on ChooseNextNode
        /// </summary>
        /// <returns> The position the ghost is headed </returns>
        protected abstract Vector2 FindTargetPosition();

        /// <summary>
        /// This calculates a random position on the board.
        /// </summary>
        /// <returns>A random position.</returns>
        protected Vector2 FindRandomPosition()
        {
            int x = Random.Range(0, 28);
            int y = Random.Range(0, 32);

            return new Vector2(x, y);
        }

        /// <summary>
        /// This method checks if the ghost is inside the ghost house and switches it back to a Normal State
        /// Called on Update
        /// </summary>
        protected void CheckIfItIsAtGhostHouse()
        {
            if (currentGhostState == GhostState.Eaten)
            {
                //Checks if the ghost is in the Ghost House
                if (previousNode == ghostHouseNode)
                {            
                    //Sets the target node, to get out of the house
                    targetNode = ghostHouseNode.neighbours[0];

                    //Sets the direction
                    direction = Vector2.up;
                    
                    //Change back to Chase State
                    requestStateChange = true;
                    ChangeState(GhostState.Chase);

                    //Call the board event
                    if (board.onGhostReachedHouse != null)
                        board.onGhostReachedHouse.Invoke();
                }
            }
        }

        #endregion

        #region Board Calculations

        /// <summary>
        /// This method checks if the ghost passed the target node he was headed
        /// </summary>
        /// <returns></returns>
        protected bool PassedNode()
        {
            //Calculates the magnitude of the vectors
            float nodeToTarget = LengthFromNode(targetNode.transform.position);
            float nodeToSelf = LengthFromNode(transform.localPosition);

            //Check if it passed
            return nodeToSelf > nodeToTarget;
        }

        /// <summary>
        /// Checks if the current node is a Portal Node
        /// </summary>
        protected void CheckForPortals()
        {
            //Try to get a portal node from the board based on the current node position
            PortalNode otherPortal = board.GetPortalNodeAtPosition(currentNode.transform.position);
            if (otherPortal != null) // in case we find one
            {
                //Teleports the ghost
                transform.localPosition = otherPortal.gameObject.transform.position;
                currentNode = otherPortal; // set the new current node
            }
        }

        /// <summary>
        /// Calculates the magnitude between two vectors
        /// </summary>
        /// <param name="targetPosition"> The position we are headed </param>
        /// <returns>The squared magnitude between the two vectors </returns>
        protected float LengthFromNode(Vector2 targetPosition)
        {
            Vector2 vec = targetPosition - (Vector2)previousNode.transform.position;
            return vec.sqrMagnitude;
        }

        /// <summary>
        /// Calculates distance between two vectors
        /// </summary>
        /// <param name="posA"> Vector 1 </param>
        /// <param name="posB"> Vector 2 </param>
        /// <returns>The Distance</returns>
        protected float GetDistance(Vector2 posA, Vector2 posB)
        {
            float dx = posA.x - posB.x;
            float dy = posA.y - posB.y;

            float distance = Mathf.Sqrt(Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2));

            return distance;
        }

        #endregion

        #region GAME BOARDS EVENTS IMPLEMENTATION

        /// <summary>
        /// Method to subscribe to the board delegates
        /// Called on Start
        /// </summary>
        protected void SubscribeToBoardEvents()
        {
            board.onGameStart += OnGameStart;
            board.onAfterGameStart += OnAfterGameStart;
            board.onGameRestart += OnGameBoardRestart;
            board.onAfterGameRestart += OnAfterGameBoardRestart;
            board.onAfterGameRestart += SetGhostsSettings;

            //Using lambda expressions for short methods
            board.onPacmanDied += delegate { ghostSprite.enabled = false; };
            board.onSuperPacpointEaten += StartFleeState;
            board.onGhostEaten += delegate { canMove = false; };
            board.onAfterGhostEaten += delegate { canMove = true; };

            board.onLevelWin += delegate
            {
                canMove = false;
                ghostSprite.enabled = false;
            };
        }

        /// <summary>
        /// Method to unsubscribe from the board delegates.
        /// Called on OnDisable
        /// </summary>
        protected void UnsubscribeFromBoardEvents()
        {
            board.onGameStart -= OnGameStart;
            board.onAfterGameStart -= OnAfterGameStart;
            board.onGameRestart -= OnGameBoardRestart;
            board.onAfterGameRestart -= OnAfterGameBoardRestart;
            board.onAfterGameRestart -= SetGhostsSettings;
            board.onPacmanDied -= delegate { ghostSprite.enabled = false; };
            board.onSuperPacpointEaten -= StartFleeState;

            board.onGhostEaten -= delegate { canMove = false; };
            board.onAfterGhostEaten -= delegate { canMove = true; };

            board.onLevelWin -= delegate
            {
                canMove = false;
                ghostSprite.enabled = false;
            };
        }

        /// <summary>
        /// Implementation of IGameBoardEvents interface
        /// Subscribes to OnGameStart board delegate
        /// </summary>
        public void OnGameStart()
        {
            //Enables sprite at game start
            ghostSprite.enabled = true;
            caughtPacman = false;

            //Init variables
            SetGhostsSettings();
        }

        /// <summary>
        /// Implementation of IGameBoardEvents interface
        /// Subscribes to OnAfterGameStart board delegate
        /// </summary>
        public void OnAfterGameStart()
        {
            //Allow movement
            canMove = true;
            //Set next destination
            targetNode = ChooseNextNode();
        }

        /// <summary>
        /// Implementation of IGameBoardEvents interface
        /// Subscribe to OnGameBoardRestart board delegate
        /// This method is virtual because every ghost has a different initialization, needs to be overrided
        /// </summary>
        public virtual void OnGameBoardRestart()
        {
            //Disable movement, sprite and request a state change
            canMove = false;
            requestStateChange = true;
            caughtPacman = false;
            ghostSprite.enabled = false;

            //Reset target destiination
            targetNode = null;
            currentNode = startingNode; //Reset to starting node
            previousNode = currentNode;

            //Reset ghost mode index to reinit the Scatter/Chase loop
            //Reset timers
            modeIndex = 0;
            timer = 0f;
            scaredTimer = 0f;

            //Set the initial position
            transform.position = startingNode.transform.localPosition;

            //Change state
            ChangeState(GhostState.Scatter);
        }

        /// <summary>
        /// Implementation of IGameBoardEvents interface
        /// Subscribe to OnAfterGameBoardRestart board delegate
        /// /// This method is virtual because every ghost has a different initial direction, needs to be overrided
        /// </summary>
        public virtual void OnAfterGameBoardRestart()
        {
            //Enables sprite
            ghostSprite.enabled = true;

            //Set direction and next destination
            direction = Vector2.left;
            targetNode = ChooseNextNode();
            previousNode = currentNode;

            //Allow movement
            canMove = true;
        }

        #endregion
    }
}
