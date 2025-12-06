using System.Collections;
using System.Collections.Generic;

namespace AleM.BehaviourTrees
{
    public class NodeBT
    {
        public enum Status { SUCCESS, RUNNING, FAILURE };
        public Status status;
        public List<NodeBT> children = new List<NodeBT>();
        public int currentChild = 0;
        public string name;
        public int sortOrder;

        public NodeBT() { }

        public NodeBT(string n)
        {
            name = n;
        }

        public NodeBT(string n, int order)
        {
            name = n;
            sortOrder = order;
        }

        public void Reset()
        {
            foreach (NodeBT n in children)
            {
                n.Reset();
            }
            currentChild = 0;
        }

        public virtual Status Process()
        {
            return children[currentChild].Process();
        }

        public void AddChild(NodeBT n)
        {
            children.Add(n);
        }

        public enum Type
        {
            Tree,
            Leaf,
            Sequence,
            Selector
        }
    }
}
