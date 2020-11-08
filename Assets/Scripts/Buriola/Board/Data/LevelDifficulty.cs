using UnityEngine;
using UnityEngine.Serialization;

namespace Buriola.Board.Data
{
    /// <summary>
    /// Represents the level difficulty
    /// </summary>
    [CreateAssetMenu(menuName = "Level Difficulty", fileName = "New Level Difficulty")]
    public class LevelDifficulty : ScriptableObject
    {
        [FormerlySerializedAs("ghostsSpeed")] 
        [Header("Ghosts Settings")]
        public float GhostsSpeed;
        [FormerlySerializedAs("ghostScaredSpeed")]
        public float GhostScaredSpeed;
        [FormerlySerializedAs("ghostsEatenSpeed")]
        public float GhostsEatenSpeed;
        [FormerlySerializedAs("pinkyReleaseTime")]
        [Space]
        public float PinkyReleaseTime;
        [FormerlySerializedAs("inkyReleaseTime")]
        public float InkyReleaseTime;
        [FormerlySerializedAs("clydeReleaseTime")]
        public float ClydeReleaseTime;

        [FormerlySerializedAs("ghostModes")] [Space]
        public AI.GhostMode[] GhostModes = new AI.GhostMode[4];
        [FormerlySerializedAs("ghostsScareDuration")] 
        [Space]
        public float GhostsScareDuration;
        [FormerlySerializedAs("ghostsStartBlinkingAt")]
        public float GhostsStartBlinkingAt;
        [FormerlySerializedAs("pacmanSpeed")] [Header("Pacman Settings")]
        public float PacmanSpeed;

        [FormerlySerializedAs("bonusItem")] 
        [Header("Bonus Items Settings")]
        public GameObject BonusItem;
    }
}
