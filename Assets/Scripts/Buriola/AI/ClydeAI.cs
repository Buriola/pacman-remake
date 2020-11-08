using UnityEngine;

namespace Buriola.AI
{
    /// <summary>
    /// This class represents Clyde, the orange ghost. Derives from Pinky
    /// </summary>
    public class ClydeAI : PinkyAI
    {
        protected override void Start()
        {
            base.Start();

            //Init direction and destination
            isInGhostHouse = true;
            direction = Vector2.up;
            targetNode = currentNode.neighbours[0];
            previousNode = currentNode;
        }

        /// <summary>
        /// Overrides the base class
        /// </summary>
        protected override void SetGhostsSettings()
        {
            base.SetGhostsSettings();

            //Gets Clyde release time
            if (board.currentLevelDifficulty != null)
                releaseTime = board.currentLevelDifficulty.clydeReleaseTime;
        }

        /// <summary>
        /// Overrides base class
        /// </summary>
        public override void OnGameBoardRestart()
        {
            base.OnGameBoardRestart();

            isInGhostHouse = true;
            releaseTimer = 0f;
        }

        /// <summary>
        /// Overrides base class
        /// </summary>
        public override void OnAfterGameBoardRestart()
        {
            ghostSprite.enabled = true;
            direction = Vector2.up;
            targetNode = currentNode.neighbours[0];
            previousNode = currentNode;
            canMove = true;
        }

        /// <summary>
        /// Clyde behaviour is quite simple. If Pacman is 8 units or more away from him, he will chase Pacman
        /// Same behaviour as Blinky
        /// Otherwise, if Pacman is close, Clyde will go to his home node, avoiding Pacman
        /// </summary>
        /// <returns>The Target position</returns>
        protected override Vector2 FindTargetPosition()
        {
            //Pacman position
            Vector2 pacmanPos = pacman.transform.localPosition;

            //Calculate distance
            float distance = GetDistance(transform.position, pacmanPos);
            Vector2 targetTile = Vector2.zero;

            if(distance > 8)
            {
                int pacmanPositionX = Mathf.RoundToInt(pacmanPos.x);
                int pacmanPositionY = Mathf.RoundToInt(pacmanPos.y);

                //Go to Pacman position
                targetTile = new Vector2(pacmanPositionX, pacmanPositionY);
            }
            else
            {
                //Go to home node
                targetTile = homeNode.transform.position;
            }

            return targetTile;
        }
    }
}