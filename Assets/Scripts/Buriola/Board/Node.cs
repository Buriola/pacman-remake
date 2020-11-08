using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pacman
{
    /// <summary>
    /// Represents a point in the board with valid directions and neighbours
    /// Used for characters board navigation
    /// </summary>
    public class Node : MonoBehaviour
    {
        //Flag to allow Pacman to move to this node
        public bool canPacmanMoveHere = true;
        //The neighbours of this node
        public Node[] neighbours;
        //The valid directions of this node
        public Vector3[] validDirections;

        protected virtual void Start()
        {
            FindValidDirections();
        }

        /// <summary>
        /// Finds all the valid directions based on how many neighbours this node has
        /// </summary>
        protected void FindValidDirections()
        {
            if (neighbours.Length <= 0)
                return;

            validDirections = new Vector3[neighbours.Length];

            for (int i = 0; i < neighbours.Length; i++)
            {
                Node neighbour = neighbours[i];
                Vector2 tempVector = neighbour.transform.localPosition - transform.localPosition;

                //Saves a normalized vector
                //Left, right, up, down
                validDirections[i] = tempVector.normalized;
            }
        }

        protected void OnDrawGizmosSelected()
        {
            //Helper
#if UNITY_EDITOR
         
            if(neighbours.Length > 0)
            {
                for (int i = 0; i < neighbours.Length; i++)
                {
                    if (neighbours[i] != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(transform.position, neighbours[i].transform.position);

                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(neighbours[i].transform.position, new Vector3(1, 1, 1));
                    }
                }
            }
#endif
        }
    }
}