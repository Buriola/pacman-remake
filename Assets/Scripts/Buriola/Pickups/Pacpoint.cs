using Buriola.Board;
using UnityEngine;
using Buriola.Interfaces;
using UnityEngine.Serialization;

namespace Buriola.Pickups
{
    public class Pacpoint : MonoBehaviour, IEatable
    {
        [FormerlySerializedAs("scoreValue")] [SerializeField]
        protected int _scoreValue = 10;
        public int ScoreValue => _scoreValue;

        public virtual void OnEaten()
        {
            GameBoard.Instance.UpdateScore(_scoreValue);
            gameObject.SetActive(false);
        }
    }
}
