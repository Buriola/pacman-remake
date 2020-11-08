using UnityEngine;

namespace Buriola.AI
{
    public sealed class InkyAI : PinkyAI
    {
        [SerializeField]
        private BlinkyAI _blinky = null;

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
                ReleaseTime = Board.CurrentLevelDifficulty.InkyReleaseTime;
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
            Vector2 pacmanPosition = Pacman.transform.localPosition;
            Vector2 pacmanDirection = Pacman.GetDirection();

            int pacmanPositionX = Mathf.RoundToInt(pacmanPosition.x);
            int pacmanPositionY = Mathf.RoundToInt(pacmanPosition.y);

            Vector2 pacmanTile = new Vector2(pacmanPositionX, pacmanPositionY);

            Vector2 targetTile = pacmanTile + (2 * pacmanDirection);

            Vector2 tempBlinkyPosition = _blinky.transform.position;
            int blinkyPosX = Mathf.RoundToInt(tempBlinkyPosition.x);
            int blinkyPosY = Mathf.RoundToInt(tempBlinkyPosition.y);

            tempBlinkyPosition = new Vector2(blinkyPosX, blinkyPosY);

            float distance = GetDistance(tempBlinkyPosition, targetTile);
            distance *= 2; // doubles it

            targetTile = new Vector2(tempBlinkyPosition.x + distance, tempBlinkyPosition.y + distance);

            return targetTile;
        }
    }
}
