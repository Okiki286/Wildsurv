using UnityEngine;

namespace AleM.BehaviourTrees
{
    public class Leaf : NodeBT
    {
        public delegate Status Tick();
        public Tick ProcessMethod;

        public delegate Status TickM(int val);
        public TickM ProcessMethodM;

        public int index;

        public Leaf() { }

        public Leaf(string n, Tick pm)
        {
            name = n;
            ProcessMethod = pm;
        }

        public Leaf(string n, int i, TickM pm)
        {
            name = n;
            ProcessMethodM = pm;
            index = i;
        }

        public Leaf(string n, Tick pm, int order)
        {
            name = n;
            ProcessMethod = pm;
            sortOrder = order;
        }

        public override Status Process()
        {
            NodeBT.Status s;
            if (ProcessMethod != null)
                s = ProcessMethod();
            else if (ProcessMethodM != null)
                s = ProcessMethodM(index);
            else
                s = Status.FAILURE;

            return s;
        }

    }
}