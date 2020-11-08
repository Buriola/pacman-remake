using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pacman.Data
{
    /// <summary>
    /// Represents the level difficulty
    /// </summary>
    [CreateAssetMenu(menuName = "Level Difficulty", fileName = "New Level Difficulty")]
    public class LevelDifficulty : ScriptableObject
    {
        [Header("Ghosts Settings")]
        public float ghostsSpeed;
        public float ghostScaredSpeed;
        public float ghostsEatenSpeed;
        [Space]
        public float pinkyReleaseTime; //Time that Pinky will be released
        public float inkyReleaseTime; //Time that Inky will be released
        public float clydeReleaseTime; // Time that Clyde will be released
        [Space]
        public AI.GhostMode[] ghostModes = new AI.GhostMode[4]; //Modes for each level
        [Space]
        public float ghostsScareDuration; //Time they will be scared
        public float ghostsStartBlinkingAt; //Time they will start blinking

        [Header("Pacman Settings")]
        public float pacmanSpeed; //Pacman speed

        [Header("Bonus Items Settings")]
        public GameObject bonusItem; //The bonus item for this level
    }
}