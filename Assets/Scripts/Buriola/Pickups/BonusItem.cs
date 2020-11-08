using Buriola.Board;
using UnityEngine;

namespace Buriola.Pickups
{
    public sealed class BonusItem : Pacpoint
    {
        private float _randomLifeExpectancy;
        private float _currentLifeTime;

        private void OnEnable()
        {
            _currentLifeTime = 0f;
            _randomLifeExpectancy = Random.Range(8f, 11f);
        }

        private void Update()
        {
            _currentLifeTime += Time.deltaTime;
            if (_currentLifeTime >= _randomLifeExpectancy)
                gameObject.SetActive(false);
        }

        public override void OnEaten()
        {
            GameBoard.Instance.UpdateScore(_scoreValue, false, true);

            GameBoard.Instance.InvokeBonusItemEaten(this);

            gameObject.SetActive(false);
        }
    }
}
