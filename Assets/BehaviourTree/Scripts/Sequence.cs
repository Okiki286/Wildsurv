

namespace AleM.BehaviourTrees
{
    public class Sequence : NodeBT
    {
        public bool loopThroughAllChildren = false;
        public Sequence(string n)
        {
            name = n;
        }
        public Sequence(string n, bool cycleThroughAll)
        {
            name = n;
            loopThroughAllChildren = cycleThroughAll;
        }

        public override Status Process()
        {
            int cycles = loopThroughAllChildren ? children.Count : 1;
            for (int i = 0; i < cycles; i++)
            {
                Status childstatus = children[currentChild].Process();
                if (childstatus == Status.RUNNING) return Status.RUNNING;
                if (childstatus == Status.FAILURE)
                {
                    Reset();
                    return childstatus;
                }

                currentChild++;
                if (currentChild >= children.Count)
                {
                    Reset();
                    return Status.SUCCESS;
                }
            }
            return Status.RUNNING;
        }
    }
}
