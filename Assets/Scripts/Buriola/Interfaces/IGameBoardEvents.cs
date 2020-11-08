namespace Buriola.Interfaces
{
    public interface IGameBoardEvents 
    {
        void OnGameStart();
        void OnAfterGameStart();
        void OnGameBoardRestart();
        void OnAfterGameBoardRestart();
        void OnGhostEaten();
        void OnAfterGhostEaten();
        void OnLevelWin();
    }
}
