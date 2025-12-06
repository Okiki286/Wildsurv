
namespace AleM.BehaviourTrees
{
    public class RSelector : NodeBT
    {
        bool shuffled = false;
        private bool loopThroughAllChildren = false;
        public RSelector(string n, bool loopThroughAll)
        {
            name = n;
            loopThroughAllChildren = loopThroughAll;
        }

        public override Status Process()
        {
            if (!shuffled)
            {
                children.Shuffle();
                shuffled = true;
            }

            int cycles = loopThroughAllChildren ? children.Count : 1;
            for (int i = 0; i < cycles; i++)
            {
                Status childstatus = children[currentChild].Process();
                if (childstatus == Status.RUNNING) return Status.RUNNING;

                if (childstatus == Status.SUCCESS)
                {
                    currentChild = 0;
                    shuffled = false;
                    return Status.SUCCESS;
                }

                currentChild++;
                if (currentChild >= children.Count)
                {
                    currentChild = 0;
                    shuffled = false;
                    return Status.FAILURE;
                }
            }
            return Status.RUNNING;
        }
    }
}
