using Buriola.Board;

namespace Buriola.Pickups
{
    public class SuperPacpoint : Pacpoint
    {
        public override void OnEaten()
        {
            base.OnEaten();

            if (GameBoard.Instance != null)
            {
                GameBoard.Instance.InvokeSuperPacpointEvent();
            }
        }
    }
}
