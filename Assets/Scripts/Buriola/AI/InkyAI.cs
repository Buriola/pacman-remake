using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pacman.AI
{
    /// <summary>
    /// This class represents Inky, the Blue Ghost. Derives from Pinky because this ghost also has a Release time
    /// behaviour
    /// </summary>
    public class InkyAI : PinkyAI
    {
        //Need a reference of the Red Ghost to calculate the next position
        [SerializeField]
        private BlinkyAI blinky = null;

        protected override void Start()
        {
            base.Start();

            //Init
            isInGhostHouse = true;
            direction = Vector2.up;
            targetNode = currentNode.neighbours[0];
            previousNode = currentNode;
        }

        protected override void SetGhostsSettings()
        {
            base.SetGhostsSettings();

            //Get Inky's release time for the current level
            if (board.currentLevelDifficulty != null)
                releaseTime = board.currentLevelDifficulty.inkyReleaseTime;
        }

        /// <summary>
        /// Overrides the base class
        /// </summary>
        public override void OnGameBoardRestart()
        {
            base.OnGameBoardRestart();
            isInGhostHouse = true;
            releaseTimer = 0f;
        }

        /// <summary>
        /// Overrides the base class
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
        /// Inky takes into account Blinky's position and Pacman's position
        /// Draws a vector from Blinkys direction + Pacman Direction and multiplies by 2
        /// </summary>
        /// <returns></returns>
        protected override Vector2 FindTargetPosition()
        {
            Vector2 pacmanPosition = pacman.transform.localPosition;
            Vector2 pacmanDirection = pacman.GetDirection();

            int pacmanPositionX = Mathf.RoundToInt(pacmanPosition.x);
            int pacmanPositionY = Mathf.RoundToInt(pacmanPosition.y);

            Vector2 pacmanTile = new Vector2(pacmanPositionX, pacmanPositionY);

            //Two tiles in from of Pacman's current direction + pacman position
            Vector2 targetTile = pacmanTile + (2 * pacmanDirection);

            Vector2 tempBlinkyPosition = blinky.transform.position;
            int blinkyPosX = Mathf.RoundToInt(tempBlinkyPosition.x);
            int blinkyPosY = Mathf.RoundToInt(tempBlinkyPosition.y);

            //Calculation happens here
            tempBlinkyPosition = new Vector2(blinkyPosX, blinkyPosY);

            //Get the distance between Blinky Position and the position calculated above
            float distance = GetDistance(tempBlinkyPosition, targetTile);
            distance *= 2; // doubles it

            //Sums it with Blinky position X and Y
            targetTile = new Vector2(tempBlinkyPosition.x + distance, tempBlinkyPosition.y + distance);

            return targetTile;
        }
    }
}