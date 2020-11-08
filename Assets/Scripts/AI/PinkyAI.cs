using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pacman.Interfaces;

namespace Pacman.AI
{
    /// <summary>
    /// This class represents the Pinky ghost or Pink Ghost. Derived from GhostAI.
    /// </summary>
    public class PinkyAI : GhostAI
    {
        //Since this ghost stays in the ghost house in the beginning
        // we set a timer to allow him to move
        protected float releaseTime;
        protected float releaseTimer = 0f;

        protected override void Start()
        {
            base.Start();

            //Set this to true
            isInGhostHouse = true;

            //Initial direction
            direction = Vector2.up;
            targetNode = ChooseNextNode();
            previousNode = currentNode;
        }

        protected override void Update()
        {
            base.Update();
            CheckTimeToRelease();
        }

        /// <summary>
        /// Overrides the base class method
        /// </summary>
        protected override void SetGhostsSettings()
        {
            base.SetGhostsSettings();

            //Only to set the release time of this ghost
            if(board.currentLevelDifficulty != null)
                releaseTime = board.currentLevelDifficulty.pinkyReleaseTime;
        }

        /// <summary>
        /// Overrides the base class
        /// </summary>
        public override void OnGameBoardRestart()
        {
            base.OnGameBoardRestart();

            //Set these variables
            isInGhostHouse = true;
            releaseTimer = 0f;
        }

        /// <summary>
        /// Overrides the base class
        /// </summary>
        public override void OnAfterGameBoardRestart()
        {
            //Different initial direction for Pinky
            ghostSprite.enabled = true;
            direction = Vector2.up;
            targetNode = ChooseNextNode();
            previousNode = currentNode;
            canMove = true;
        }

        /// <summary>
        /// Trigger a flag to stop the timer and allow movement for the ghost
        /// </summary>
        private void Release()
        {
            if (isInGhostHouse)
            {
                isInGhostHouse = false;
            }
        }

        /// <summary>
        /// Increments the timer and release the ghost after a delay
        /// </summary>
        protected void CheckTimeToRelease()
        {
            if(canMove)
                releaseTimer += Time.deltaTime;

            if (releaseTimer > releaseTime)
                Release();
        }

        /// <summary>
        /// Pinky tries to find a position 4 tiles in front of Pacman, based on his direction
        /// Ambush behaviour
        /// </summary>
        /// <returns>The target position</returns>
        protected override Vector2 FindTargetPosition()
        {
            //Get Pacman's position and current direction
            Vector2 pacmanPos = pacman.gameObject.transform.localPosition;
            Vector2 pacmanDirection = pacman.GetDirection();

            //Round it
            int pacmanPositionX = Mathf.RoundToInt(pacmanPos.x);
            int pacmanPositionY = Mathf.RoundToInt(pacmanPos.y);

            //Calculates a position 4 times ahead of Pacman's current direction
            Vector2 pacmanTile = new Vector2(pacmanPositionX, pacmanPositionY);
            Vector2 targetTile = pacmanTile + (4 * pacmanDirection);

            return targetTile;
        }
    }
}