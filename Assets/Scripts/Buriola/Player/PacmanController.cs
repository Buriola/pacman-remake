using System.Collections;
using Buriola.Board;
using UnityEngine;
using Buriola.Interfaces;
using UnityEngine.Serialization;

namespace Buriola.Player
{
    public class PacmanController : MonoBehaviour, IGameBoardEvents
    {
        #region Variables
        private bool _isDead;
        private bool _canMove;
        private bool _swapSound;

        [FormerlySerializedAs("moveSpeed")] 
        [SerializeField]
        private float _moveSpeed = 6f;
        public int KillStreak { get; set; }
        private Vector2 _direction;
        private Vector2 _nextDirection;
        
        [FormerlySerializedAs("startingNode")] 
        [SerializeField]
        private Node _startingNode = null; //Starting position
        private Node _currentNode; 
        private Node _targetNode;
        private Node _previousNode;

        public SpriteRenderer PacmanSprite { get; private set; }
        private Animator _anim;
        private GameBoard _board;
        private AudioSource _audioSource;

        [FormerlySerializedAs("pacmanDeathSound")] 
        [SerializeField]
        private AudioClip _pacmanDeathSound = null;
        [FormerlySerializedAs("chompOneSound")] 
        [SerializeField]
        private AudioClip _chompOneSound = null;
        [FormerlySerializedAs("chompTwoSound")] 
        [SerializeField]
        private AudioClip _chompTwoSound = null;

        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int NormalTrigger = Animator.StringToHash("NormalTrigger");
        private static readonly int Horizontal = Animator.StringToHash("Horizontal");
        private static readonly int Vertical = Animator.StringToHash("Vertical");
        private static readonly int DeathTrigger = Animator.StringToHash("DeathTrigger");

        #endregion

        private void Init()
        {
            _direction = Vector2.left;
            KillStreak = 1;

            PacmanSprite = GetComponentInChildren<SpriteRenderer>();
            _anim = GetComponentInChildren<Animator>();
            _board = FindObjectOfType<GameBoard>();
            _audioSource = GetComponent<AudioSource>();
            PacmanSprite.enabled = false;

            if (_startingNode != null)
            {
                _currentNode = _startingNode;
                transform.position = _startingNode.transform.position;
            }

            ChangeDirection(_direction);
            SubscribeToBoardEvents();
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
            IEatable eatable = other.GetComponent<IEatable>();
            if (eatable != null)
            {
                eatable.OnEaten();
                PlayChompSound();
            }
        }
        #endregion

        #region Movement Related
        private void GetInput()
        {
            if (Input.GetAxis("Horizontal") > 0) ChangeDirection(Vector2.right);
            if (Input.GetAxis("Horizontal") < 0) ChangeDirection(Vector2.left);
            if (Input.GetAxis("Vertical") > 0) ChangeDirection(Vector2.up);
            if (Input.GetAxis("Vertical") < 0) ChangeDirection(Vector2.down);
        }

        private void Movement()
        {
            if (_isDead || !_canMove)
                return;

            if (_targetNode != _currentNode && _targetNode != null)
            {
                if (_nextDirection == _direction * -1)
                {
                    _direction *= -1;
                    
                    Node tempNode = _targetNode;
                    _targetNode = _previousNode;
                    _previousNode = tempNode;
                }

                if (PassedNode())
                {
                    _currentNode = _targetNode;
                    transform.localPosition = _currentNode.transform.position;

                    CheckForPortals();

                    Node moveToNode = CanMove(_nextDirection);

                    if (moveToNode != null)
                    {
                        _direction = _nextDirection;
                    }

                    if (moveToNode == null)
                        moveToNode = CanMove(_direction);

                    if (moveToNode != null)
                    {
                        _targetNode = moveToNode;
                        _previousNode = _currentNode;
                        _currentNode = null;
                    }
                    else
                    {
                        if (_anim.GetBool(IsMoving) == true)
                            _anim.SetBool(IsMoving, false);
                    }
                }
                else
                {
                    if (_anim.GetBool(IsMoving) == false)
                        _anim.SetBool(IsMoving, true);

                    transform.position += (Vector3)(_direction * _moveSpeed) * Time.deltaTime;
                }
            }
        }

        private void ChangeDirection(Vector2 direction)
        {
            if (direction != _direction)
                _nextDirection = direction;

            if (_currentNode != null)
            {
                Node moveToNode = CanMove(direction);
                if (moveToNode != null)
                {
                    _direction = direction;
                    _targetNode = moveToNode;
                    _previousNode = _currentNode;
                    _currentNode = null;
                }
            }
        }

        private Node CanMove(Vector2 d)
        {
            Node moveToNode = null;

            for (int i = 0; i < _currentNode.Neighbours.Length; i++)
            {
                if (_currentNode.ValidDirections[i] == (Vector3)d && _currentNode.Neighbours[i].CanPacmanMoveHere)
                {
                    moveToNode = _currentNode.Neighbours[i];
                    break;
                }
            }

            return moveToNode;
        }

        private void UpdatePacmanSpeed()
        {
            if (_board.CurrentLevelDifficulty != null)
                _moveSpeed = _board.CurrentLevelDifficulty.PacmanSpeed;
        }

        public Vector2 GetDirection()
        {
            return _direction;
        }

        private void HandleAnimations()
        {
            _anim.SetFloat(Horizontal, _direction.x);
            _anim.SetFloat(Vertical, _direction.y);
        }
        #endregion

        #region Board Calculations and Events
        public void StartDeath()
        {
            _isDead = true;
            StartCoroutine(Death());
        }
        
        private IEnumerator Death()
        {
            yield return new WaitForSeconds(1f);

            _board.InvokeOnPacmanDiedEvent();
            
            _audioSource.PlayOneShot(_pacmanDeathSound);
            _anim.SetTrigger(DeathTrigger);

            yield return new WaitForSeconds(3f);

            _board.StartRestart();
        }

        private void ResetKillStreak()
        {
            KillStreak = 1;
        }

        private void CheckForPortals()
        {
            PortalNode otherPortal = _board.GetPortalNodeAtPosition(_currentNode.transform.position);
            if (otherPortal != null)
            {
                transform.localPosition = otherPortal.gameObject.transform.position;
                _currentNode = otherPortal;
            }
        }

        private bool PassedNode()
        {
            float nodeToTarget = LengthFromNode(_targetNode.transform.position);
            float nodeToSelf = LengthFromNode(transform.localPosition);

            return nodeToSelf > nodeToTarget;
        }

        private float LengthFromNode(Vector2 targetPosition)
        {
            Vector2 vec = targetPosition - (Vector2)_previousNode.transform.position;
            return vec.sqrMagnitude;
        }

        private void PlayChompSound()
        {
            if (_swapSound)
            {
                _audioSource.PlayOneShot(_chompTwoSound);
                _swapSound = false;
            }
            else
            {
                _audioSource.PlayOneShot(_chompOneSound);
                _swapSound = false;
            }
        }
        #endregion

        #region GAME BOARD EVENTS IMPLEMENTATION

        private void SubscribeToBoardEvents()
        {
            _board.OnGameStart += OnGameStart;
            _board.OnAfterGameStart += OnAfterGameStart;
            _board.OnGameRestart += OnGameBoardRestart;
            _board.OnAfterGameRestart += OnAfterGameBoardRestart;
            _board.OnAfterGameRestart += UpdatePacmanSpeed;

            _board.OnSuperPacpointEaten += ResetKillStreak;
            _board.OnGhostEaten += OnGhostEaten;
            _board.OnAfterGhostEaten += OnAfterGhostEaten;
            _board.OnLevelWin += OnLevelWin;
        }
        
        private void UnsubscribeFromBoardEvents()
        {
            _board.OnGameStart -= OnGameStart;
            _board.OnAfterGameStart -= OnAfterGameStart;
            _board.OnGameRestart -= OnGameBoardRestart;
            _board.OnAfterGameRestart -= OnAfterGameBoardRestart;
            _board.OnAfterGameRestart -= UpdatePacmanSpeed;

            _board.OnSuperPacpointEaten -= ResetKillStreak;
            _board.OnGhostEaten -= OnGhostEaten;
            _board.OnAfterGhostEaten -= OnAfterGhostEaten;

            _board.OnLevelWin -= OnLevelWin;
        }

        public void OnGameStart()
        {
            PacmanSprite.enabled = true;
        }

        public void OnAfterGameStart()
        {
            _canMove = true;
        }

        public void OnGameBoardRestart()
        {
            _isDead = false;
            _canMove = false;
            KillStreak = 1;

            _anim.SetBool(IsMoving, false);
            _anim.SetTrigger(NormalTrigger);

            _direction = Vector2.left;
            _nextDirection = Vector2.left;
            _currentNode = _startingNode;

            transform.position = _startingNode.transform.position;
            ChangeDirection(_direction);
        }

        public void OnAfterGameBoardRestart()
        {
            _canMove = true;
            PacmanSprite.enabled = true;
        }

        public void OnGhostEaten()
        {
            _canMove = false;
        }

        public void OnAfterGhostEaten()
        {
            _canMove = true;
        }

        public void OnLevelWin()
        {
            _canMove = false;
            PacmanSprite.enabled = false;
        }

        #endregion
    }
}
