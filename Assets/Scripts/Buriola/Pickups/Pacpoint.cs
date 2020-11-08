﻿using Buriola.Board;
using UnityEngine;
using Buriola.Interfaces;

namespace Buriola.Pickups
{
    /// <summary>
    /// Represents a Pacpoint/Dot/Pellet that Pacman can eat
    /// </summary>
    public class Pacpoint : MonoBehaviour, IEatable
    {
        //The default score value for eating this
        [SerializeField]
        protected int scoreValue = 10;

        //A getter
        public int ScoreValue { get { return scoreValue; } }

        /// <summary>
        /// Implementation of interface
        /// </summary>
        public virtual void OnEaten()
        {
            //Updates the score
            GameBoard.Instance.UpdateScore(scoreValue);
            //Deactivate the object
            gameObject.SetActive(false);
        }
    }
}
