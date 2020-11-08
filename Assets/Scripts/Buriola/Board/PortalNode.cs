using UnityEngine.Serialization;

namespace Buriola.Board
{
    public class PortalNode : Node
    {
        //The other portal we supposed to come out
        [FormerlySerializedAs("portalReceiver")] 
        public PortalNode PortalReceiver;
    }
}
