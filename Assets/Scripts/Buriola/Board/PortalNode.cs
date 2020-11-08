namespace Buriola.Board
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
