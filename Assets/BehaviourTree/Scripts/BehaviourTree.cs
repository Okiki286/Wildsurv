using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AleM.BehaviourTrees
{
    public class BehaviourTree : NodeBT
    {
        public BehaviourTree()
        {
            name = "Tree";
        }

        public BehaviourTree(string n)
        {
            name = n;
        }

        public override Status Process()
        {
            if (children.Count == 0) return Status.SUCCESS;
            return children[currentChild].Process();
        }

        struct NodeLevel
        {
            public int level;
            public NodeBT node;
        }

        public string PrintTree()
        {
            string treePrintout = "";
            Stack<NodeLevel> nodeStack = new Stack<NodeLevel>();
            NodeBT currentNode = this;
            nodeStack.Push(new NodeLevel { level = 0, node = currentNode });

            while (nodeStack.Count != 0)
            {
                NodeLevel nextNode = nodeStack.Pop();
                treePrintout += new string('-', nextNode.level) + nextNode.node.name + "\n";
                for (int i = nextNode.node.children.Count - 1; i >= 0; i--)
                {
                    nodeStack.Push(new NodeLevel { level = nextNode.level + 1, node = nextNode.node.children[i] });
                }
            }

            Debug.Log(treePrintout);
            return treePrintout;
        }

    }
}
