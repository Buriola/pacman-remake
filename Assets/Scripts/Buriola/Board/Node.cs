using UnityEngine;
using UnityEngine.Serialization;

namespace Buriola.Board
{
    public class Node : MonoBehaviour
    {
        [FormerlySerializedAs("canPacmanMoveHere")] 
        public bool CanPacmanMoveHere = true;
        [FormerlySerializedAs("neighbours")] 
        public Node[] Neighbours;
        [FormerlySerializedAs("validDirections")] 
        public Vector3[] ValidDirections;

        protected virtual void Start()
        {
            FindValidDirections();
        }

        private void FindValidDirections()
        {
            if (Neighbours.Length <= 0)
                return;

            ValidDirections = new Vector3[Neighbours.Length];

            for (int i = 0; i < Neighbours.Length; i++)
            {
                Node neighbour = Neighbours[i];
                Vector2 tempVector = neighbour.transform.localPosition - transform.localPosition;

                ValidDirections[i] = tempVector.normalized;
            }
        }

        protected void OnDrawGizmosSelected()
        {
            //Helper
#if UNITY_EDITOR
         
            if(Neighbours.Length > 0)
            {
                for (int i = 0; i < Neighbours.Length; i++)
                {
                    if (Neighbours[i] != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(transform.position, Neighbours[i].transform.position);

                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(Neighbours[i].transform.position, new Vector3(1, 1, 1));
                    }
                }
            }
#endif
        }
    }
}
