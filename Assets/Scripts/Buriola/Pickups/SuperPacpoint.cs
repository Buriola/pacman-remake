using Buriola.Board;

namespace Buriola.Pickups
{
    public class SuperPacpoint : Pacpoint
    {
        public override void OnEaten()
        {
            //Same as base
            base.OnEaten();

            if (GameBoard.Instance != null)
            {
                //But triggers the board event to scare ghosts
                if(GameBoard.Instance.onSuperPacpointEaten != null)
                    GameBoard.Instance.onSuperPacpointEaten.Invoke();
            }
        }
    }
}
