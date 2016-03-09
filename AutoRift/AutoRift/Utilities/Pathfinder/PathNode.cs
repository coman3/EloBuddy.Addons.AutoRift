namespace AutoRift.Utilities.Pathfinder
{
    internal class PathNode
    {
        public double GCost;
        public double HCost;
        public PathNode Parent;
        public int Node;
        public double FCost { get { return GCost + HCost; } }

        public PathNode(double gCost, double hCost, int node, PathNode parent)
        {
            Parent = parent;
            this.Node = node;
            this.GCost = gCost;
            this.HCost = hCost;
        }
    }
}
