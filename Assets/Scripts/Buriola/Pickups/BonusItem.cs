using Buriola.Board;
using UnityEngine;

namespace Buriola.Pickups
{
    /// <summary>
    /// Represents a Bonus Item. Derives from Pacpoint
    /// </summary>
    public class BonusItem : Pacpoint
    {
        //A random value this item will be available on the level
        private float randomLifeExpectancy;
        private float currentLifeTime; //Timer

        private void OnEnable()
        {
            //Everytime we enable the object, reset the timer
            currentLifeTime = 0f;
            //Get a random value between 8 and 11 seconds
            randomLifeExpectancy = Random.Range(8f, 11f);
        }

        private void Update()
        {
            //Updates the timer and disables the object after the time has passed
            currentLifeTime += Time.deltaTime;
            if (currentLifeTime >= randomLifeExpectancy)
                gameObject.SetActive(false);
        }

        public override void OnEaten()
        {
            //Update the score
            GameBoard.Instance.UpdateScore(scoreValue, false, true);

            //Call board event
            if(GameBoard.Instance.onBonusItemEaten != null)
                GameBoard.Instance.onBonusItemEaten(this);

            //Disable object
            gameObject.SetActive(false);
        }
    }
}
