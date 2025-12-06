
namespace AleM.BehaviourTrees
{
    public class Selector : NodeBT
    {
        public bool loopThroughAllChildren = false; 

        public Selector(string n)
        {
            name = n;
        }
        public Selector(string n, bool cycleThroughAll)
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

                if (childstatus == Status.SUCCESS)
                {
                    Reset();
                    return Status.SUCCESS;
                }

                currentChild++;
                if (currentChild >= children.Count)
                {
                    Reset();
                    return Status.FAILURE;
                }
            }
            return Status.RUNNING;
        }
    }
}