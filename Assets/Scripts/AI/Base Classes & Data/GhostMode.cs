using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pacman.AI
{
    /// <summary>
    /// This class represents a ghost mode
    /// During the game the ghost alter between Chase mode and Scatter mode.
    /// </summary>
    [System.Serializable]
    public class GhostMode
    {
        //In seconds
        public float scatterModeTime; //How long it will scatter
        public float chaseModeTime; //How long it will chase

        public GhostMode(float scatterModeTime, float chaseModeTime)
        {
            this.scatterModeTime = scatterModeTime;
            this.chaseModeTime = chaseModeTime;
        }
    }
}