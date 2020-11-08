using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pacman.Interfaces
{
    /// <summary>
    /// Allows collision with Pacman
    /// Every object that can be eaten by Pacman, must implement this interface
    /// </summary>
    public interface IEatable
    {
        void OnEaten();
    }
}