using UnityEngine;

namespace Buriola.AI
{
    /// <summary>
    /// This class represents the Blinky ghost or Red Ghost. Derived from the Ghost class.
    /// </summary>
    public class BlinkyAI : GhostAI
    {
        protected override void Start()
        {
            base.Start();

            //Set the initial direction
            direction = Vector2.left;
            targetNode = currentNode.neighbours[0];
            previousNode = currentNode;
        }

        /// <summary>
        /// Blinky always goes to the Pacman position while on Chase mode
        /// </summary>
        /// <returns>Returns the target position</returns>
        protected override Vector2 FindTargetPosition()
        {
            //Get Pacman position
            Vector2 pacmanPos = pacman.gameObject.transform.position;
            //Round it
            Vector2 targetTile = new Vector2(Mathf.RoundToInt(pacmanPos.x), Mathf.RoundToInt(pacmanPos.y));

            //Return it
            return targetTile;
        }
    }
}
