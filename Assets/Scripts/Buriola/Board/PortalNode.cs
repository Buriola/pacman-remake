using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pacman
{
    /// <summary>
    /// Represents a portal node. Derives from Node
    /// </summary>
    public class PortalNode : Node
    {
        //The other portal we supposed to come out
        public PortalNode portalReceiver;
    }
}