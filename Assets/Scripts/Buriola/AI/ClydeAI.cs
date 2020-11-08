using UnityEngine;

namespace Buriola.AI
{
    public sealed class ClydeAI : PinkyAI
    {
        protected override void Start()
        {
            base.Start();

            IsInGhostHouse = true;
            Direction = Vector2.up;
            TargetNode = CurrentNode.Neighbours[0];
            PreviousNode = CurrentNode;
        }

        protected override void SetGhostsSettings()
        {
            base.SetGhostsSettings();

            if (Board.CurrentLevelDifficulty != null)
                ReleaseTime = Board.CurrentLevelDifficulty.ClydeReleaseTime;
        }

        public override void OnGameBoardRestart()
        {
            base.OnGameBoardRestart();

            IsInGhostHouse = true;
            ReleaseTimer = 0f;
        }

        public override void OnAfterGameBoardRestart()
        {
            GhostSprite.enabled = true;
            Direction = Vector2.up;
            TargetNode = CurrentNode.Neighbours[0];
            PreviousNode = CurrentNode;
            CanMove = true;
        }

        protected override Vector2 FindTargetPosition()
        {
            Vector2 pacmanPos = Pacman.transform.localPosition;

            float distance = GetDistance(transform.position, pacmanPos);
            Vector2 targetTile = Vector2.zero;

            if(distance > 8)
            {
                int pacmanPositionX = Mathf.RoundToInt(pacmanPos.x);
                int pacmanPositionY = Mathf.RoundToInt(pacmanPos.y);

                targetTile = new Vector2(pacmanPositionX, pacmanPositionY);
            }
            else
            {
                targetTile = HomeNode.transform.position;
            }

            return targetTile;
        }
    }
}