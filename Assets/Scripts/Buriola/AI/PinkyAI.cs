using UnityEngine;

namespace Buriola.AI
{
    public class PinkyAI : GhostAI
    {
        protected float ReleaseTime;
        protected float ReleaseTimer;

        protected override void Start()
        {
            base.Start();

            IsInGhostHouse = true;

            Direction = Vector2.up;
            TargetNode = ChooseNextNode();
            PreviousNode = CurrentNode;
        }

        protected override void Update()
        {
            base.Update();
            CheckTimeToRelease();
        }

        protected override void SetGhostsSettings()
        {
            base.SetGhostsSettings();

            if(Board.CurrentLevelDifficulty != null)
                ReleaseTime = Board.CurrentLevelDifficulty.PinkyReleaseTime;
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
            TargetNode = ChooseNextNode();
            PreviousNode = CurrentNode;
            CanMove = true;
        }

        private void Release()
        {
            if (IsInGhostHouse)
            {
                IsInGhostHouse = false;
            }
        }

        private void CheckTimeToRelease()
        {
            if(CanMove)
                ReleaseTimer += Time.deltaTime;

            if (ReleaseTimer > ReleaseTime)
                Release();
        }

        protected override Vector2 FindTargetPosition()
        {
            Vector2 pacmanPos = Pacman.gameObject.transform.localPosition;
            Vector2 pacmanDirection = Pacman.GetDirection();

            int pacmanPositionX = Mathf.RoundToInt(pacmanPos.x);
            int pacmanPositionY = Mathf.RoundToInt(pacmanPos.y);

            Vector2 pacmanTile = new Vector2(pacmanPositionX, pacmanPositionY);
            Vector2 targetTile = pacmanTile + (4 * pacmanDirection);

            return targetTile;
        }
    }
}
