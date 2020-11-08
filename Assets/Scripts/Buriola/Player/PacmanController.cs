using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pacman.Interfaces;

namespace Pacman.Controllers
{
    /// <summary>
    /// Represents Pacman and receives User Input. Implements the IGameBoardEvents
    /// </summary>
    public class PacmanController : MonoBehaviour, IGameBoardEvents
    {
        #region Variables
        private bool isDead; //Dead flag
        private bool canMove; //Can move flag
        private bool swapSound; //Swap chomp sound flag

        [SerializeField]
        private float moveSpeed = 6f; //Current move speed
        public int KillStreak { get; set; } //Killstreak for eating ghosts
       
        //Directions
        private Vector2 previousDirection;
        private Vector2 direction = Vector2.zero;
        private Vector2 nextDirection = Vector2.zero;

        //Navigation nodes
        [SerializeField]
        private Node startingNode = null; //Starting position
        private Node currentNode; 
        private Node targetNode;
        private Node previousNode;

        //References
        [HideInInspector]
        public SpriteRenderer pacmanSprite;
        private Animator anim;
        private GameBoard board;
        private AudioSource audioS;

        //Audio related
        [SerializeField]
        private AudioClip pacmanDeathSound = null;
        [SerializeField]
        private AudioClip chompOneSound = null;
        [SerializeField]
        private AudioClip chompTwoSound = null;
        #endregion

        /// <summary>
        /// Initializes Pacman Character
        /// Called on Start
        /// </summary>
        private void Init()
        {
            //Set variables
            direction = Vector2.left;
            KillStreak = 1;

            //Get references
            pacmanSprite = GetComponentInChildren<SpriteRenderer>();
            anim = GetComponentInChildren<Animator>();
            board = FindObjectOfType<GameBoard>();
            audioS = GetComponent<AudioSource>();
            pacmanSprite.enabled = false;

            //Starting position
            if (startingNode != null)
            {
                currentNode = startingNode;
                transform.position = startingNode.transform.position;
            }

            //Change direction
            ChangeDirection(direction);
            //Subscribe to events
            SubscribeToBoardEvents();
            //Updates his speed based on the level difficulty
            UpdatePacmanSpeed();
        }

        #region UNITY FUNCTIONS
        private void Start()
        {
            Init();
        }

        private void Update()
        {
            GetInput();
            Movement();
            HandleAnimations();
        }

        private void OnDisable()
        {
            UnsubscribeFromBoardEvents();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            //With every object we collide
            //Try to find out if it implements the IEatable interface
            IEatable eatable = other.GetComponent<IEatable>();

            // if it does
            if (eatable != null)
            {
                //Call interface method
                eatable.OnEaten();

                //Play sound
                PlayChompSound();
            }
        }
        #endregion

        #region Movement Related
        /// <summary>
        /// Gets input from the player and updates direction
        /// </summary>
        private void GetInput()
        {
            if (Input.GetAxis("Horizontal") > 0) ChangeDirection(Vector2.right);
            if (Input.GetAxis("Horizontal") < 0) ChangeDirection(Vector2.left);
            if (Input.GetAxis("Vertical") > 0) ChangeDirection(Vector2.up);
            if (Input.GetAxis("Vertical") < 0) ChangeDirection(Vector2.down);
        }

        /// <summary>
        /// Handles Pacman movement
        /// </summary>
        private void Movement()
        {
            //No point in moving if these flags are set
            if (isDead || !canMove)
                return;

            //Check to see if the destination is not null
            if (targetNode != currentNode && targetNode != null)
            {
                //Pacman is allowed to do switchbacks
                //Going left then suddently change direction to right
                if (nextDirection == direction * -1)
                {
                    //Invert direction
                    direction *= -1;
                    //Set nodes
                    Node tempNode = targetNode;
                    targetNode = previousNode;
                    previousNode = tempNode;
                }

                //Checks to see if Pacman passed the target node he was headed
                if (PassedNode())
                {
                    //In that case we update the current node
                    currentNode = targetNode;
                    transform.localPosition = currentNode.transform.position; // set his position

                    //Check for portal node in this position, in that case, will be teleported
                    CheckForPortals();

                    //Check if we can move in the next direction Pacman wants to go
                    //If true, we get that node
                    Node moveToNode = CanMove(nextDirection);

                    //If we get that node
                    if (moveToNode != null)
                    {
                        //Updates direction
                        previousDirection = direction;
                        direction = nextDirection;
                    }

                    //in case we dont have a NextDirection, still moving in the same direction
                    if (moveToNode == null)
                        moveToNode = CanMove(direction); // check to see if there is a valid node in that direction

                    //If so
                    if (moveToNode != null)
                    {
                        //Set our target node
                        targetNode = moveToNode;
                        previousNode = currentNode;
                        currentNode = null; // since Pacman is between nodes, set this to null
                    }
                    else // set the idle anim if Pacman is not moving
                    {
                        if (anim.GetBool("IsMoving") == true)
                            anim.SetBool("IsMoving", false);
                    }
                }
                else
                {
                    //set movement animation
                    if (anim.GetBool("IsMoving") == false)
                        anim.SetBool("IsMoving", true);

                    //Move Pacman in the current direction
                    transform.position += (Vector3)(direction * moveSpeed) * Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// Set the next direction Pacman wants to go
        /// </summary>
        /// <param name="d">The desired direction </param>
        private void ChangeDirection(Vector2 d)
        {
            //if it is not the same as the current direction
            if (d != direction)
                nextDirection = d; // set the next direction

            //If we hit a node
            if (currentNode != null)
            {
                //Check to see if we can move in this next direction
                Node moveToNode = CanMove(d);
                if (moveToNode != null)
                {
                    //If so, set the current direction and target node
                    direction = d;
                    targetNode = moveToNode;
                    previousNode = currentNode;
                    currentNode = null;
                }
            }
        }

        /// <summary>
        /// Check if Pacman can move to a direction
        /// </summary>
        /// <param name="d">Returns the node from that valid direction</param>
        /// <returns>Null or the next valid node</returns>
        private Node CanMove(Vector2 d)
        {
            Node moveToNode = null;

            //Check the neighbours of the current neighbour
            for (int i = 0; i < currentNode.neighbours.Length; i++)
            {
                //If Pacman current direction is valid for this next nodes and Pacman can move to this node
                if (currentNode.validDirections[i] == (Vector3)d && currentNode.neighbours[i].canPacmanMoveHere)
                {
                    // then we set the next node
                    moveToNode = currentNode.neighbours[i];
                    break; // get out, already found a node
                }
            }

            return moveToNode;
        }

        /// <summary>
        /// Updates Pacman move speed based in the current level difficulty
        /// </summary>
        private void UpdatePacmanSpeed()
        {
            if (board.currentLevelDifficulty != null)
                moveSpeed = board.currentLevelDifficulty.pacmanSpeed;
        }

        /// <summary>
        /// Gets Pacman current direction
        /// </summary>
        /// <returns></returns>
        public Vector2 GetDirection()
        {
            return direction;
        }

        /// <summary>
        /// Updates the animator variables
        /// </summary>
        private void HandleAnimations()
        {
            anim.SetFloat("Horizontal", direction.x);
            anim.SetFloat("Vertical", direction.y);
        }
        #endregion

        #region Board Calculations and Events
        /// <summary>
        /// Triggers Death coroutine
        /// </summary>
        public void StartDeath()
        {
            isDead = true;
            StartCoroutine(Death());
        }
        /// <summary>
        /// Handles Pacman death
        /// </summary>
        /// <returns></returns>
        private IEnumerator Death()
        {
            yield return new WaitForSeconds(1f);

            //Call board event
            
            board.onPacmanDied?.Invoke();

            //Play death sound and animation
            audioS.PlayOneShot(pacmanDeathSound);
            anim.SetTrigger("DeathTrigger");

            yield return new WaitForSeconds(3f);

            //Trigger a restart
            board.StartRestart();
        }

        /// <summary>
        /// Reset Pacman killstreak to 1
        /// </summary>
        private void ResetKillStreak()
        {
            KillStreak = 1;
        }

        /// <summary>
        /// Checks if the current node is a Portal Node
        /// </summary>
        private void CheckForPortals()
        {
            // Try to get a portal node from the board based on the current node position
            PortalNode otherPortal = board.GetPortalNodeAtPosition(currentNode.transform.position);
            if (otherPortal != null)
            {
                //Teleports Pacman
                transform.localPosition = otherPortal.gameObject.transform.position;
                currentNode = otherPortal;
            }
        }

        /// <summary>
        /// This method checks if Pacman passed the target node he was headed
        /// </summary>
        /// <returns></returns>
        private bool PassedNode()
        {
            float nodeToTarget = LengthFromNode(targetNode.transform.position);
            float nodeToSelf = LengthFromNode(transform.localPosition);

            return nodeToSelf > nodeToTarget;
        }

        /// <summary>
        /// Calculates the magnitude between two vectors
        /// </summary>
        /// <param name="targetPosition"> The position we are headed </param>
        /// <returns>The squared magnitude between the two vectors </returns>
        private float LengthFromNode(Vector2 targetPosition)
        {
            Vector2 vec = targetPosition - (Vector2)previousNode.transform.position;
            return vec.sqrMagnitude;
        }

        /// <summary>
        /// Plays chomp sound alternating with flag
        /// </summary>
        private void PlayChompSound()
        {
            if (swapSound)
            {
                audioS.PlayOneShot(chompTwoSound);
                swapSound = false;
            }
            else
            {
                audioS.PlayOneShot(chompOneSound);
                swapSound = false;
            }
        }
        #endregion

        #region GAME BOARD EVENTS IMPLEMENTATION

        /// <summary>
        /// Subscribe to board events
        /// </summary>
        private void SubscribeToBoardEvents()
        {
            board.onGameStart += OnGameStart;
            board.onAfterGameStart += OnAfterGameStart;
            board.onGameRestart += OnGameBoardRestart;
            board.onAfterGameRestart += OnAfterGameBoardRestart;
            board.onAfterGameRestart += UpdatePacmanSpeed;

            board.onSuperPacpointEaten += ResetKillStreak;
            board.onGhostEaten += delegate { canMove = false; };
            board.onAfterGhostEaten += delegate { canMove = true; };
            board.onLevelWin += delegate
            {
                canMove = false;
                pacmanSprite.enabled = false;
            };
        }
        /// <summary>
        /// Unsubscribe from board events
        /// </summary>
        private void UnsubscribeFromBoardEvents()
        {
            board.onGameStart -= OnGameStart;
            board.onAfterGameStart -= OnAfterGameStart;
            board.onGameRestart -= OnGameBoardRestart;
            board.onAfterGameRestart -= OnAfterGameBoardRestart;
            board.onAfterGameRestart -= UpdatePacmanSpeed;

            board.onSuperPacpointEaten -= ResetKillStreak;
            board.onGhostEaten -= delegate { canMove = false; };
            board.onAfterGhostEaten -= delegate { canMove = true; };

            board.onLevelWin -= delegate
            {
                canMove = false;
                pacmanSprite.enabled = false;
            };
        }

        /// <summary>
        /// Implemenation of IGameBoardEvents interface
        /// </summary>
        public void OnGameStart()
        {
            pacmanSprite.enabled = true;
        }

        /// <summary>
        /// Implemenation of IGameBoardEvents interface
        /// </summary>
        public void OnAfterGameStart()
        {
            canMove = true;
        }

        /// <summary>
        /// Implemenation of IGameBoardEvents interface
        /// </summary>
        public void OnGameBoardRestart()
        {
            isDead = false;
            canMove = false;
            KillStreak = 1;

            anim.SetBool("IsMoving", false);
            anim.SetTrigger("NormalTrigger");

            direction = Vector2.left;
            nextDirection = Vector2.left;
            currentNode = startingNode;

            transform.position = startingNode.transform.position;
            ChangeDirection(direction);
        }

        /// <summary>
        /// Implemenation of IGameBoardEvents interface
        /// </summary>
        public void OnAfterGameBoardRestart()
        {
            canMove = true;
            pacmanSprite.enabled = true;
        }

        #endregion
    }
}