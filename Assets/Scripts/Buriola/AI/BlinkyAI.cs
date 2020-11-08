using UnityEngine;

namespace Buriola.AI
{
    public class BlinkyAI : GhostAI
    {
        protected override void Start()
        {
            base.Start();

            Direction = Vector2.left;
            TargetNode = CurrentNode.Neighbours[0];
            PreviousNode = CurrentNode;
        }

        protected override Vector2 FindTargetPosition()
        {
            Vector2 pacmanPos = Pacman.gameObject.transform.position;
            Vector2 targetTile = new Vector2(Mathf.RoundToInt(pacmanPos.x), Mathf.RoundToInt(pacmanPos.y));

            return targetTile;
        }
    }
}
