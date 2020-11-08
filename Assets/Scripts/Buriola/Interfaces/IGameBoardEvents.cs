namespace Buriola.Interfaces
{
    public interface IGameBoardEvents 
    {
        void OnGameStart();
        void OnAfterGameStart();
        void OnGameBoardRestart();
        void OnAfterGameBoardRestart();
    }
}
