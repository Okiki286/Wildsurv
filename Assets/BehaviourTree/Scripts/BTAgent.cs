using AleM.BehaviourTrees;
using System.Collections;
using UnityEngine;

public class BTAgent : MonoBehaviour
{
    [SerializeField] private BehaviourTreeContainer treeContainer;

    public BehaviourTree tree;
    protected NodeBT.Status treeStatus = NodeBT.Status.RUNNING;

    protected WaitForSeconds waitForSeconds;
    protected private float waitTime;
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (treeContainer == null) return;

        // Check if all methods are present on this agent
        foreach (BTNodeData nodeData in treeContainer.nodeData)
        {
            if (string.IsNullOrEmpty(nodeData.leafMethod)) continue;

            if (GetType().GetMethod(nodeData.leafMethod,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) == null)
            {
                treeContainer = null;
                Debug.LogError("Could not find (" + nodeData.leafMethod + ") in " + name);
                return;
            }
        }
#endif
    }
    protected virtual void Start()
    {
        if (!treeContainer) throw new System.Exception("ASSIGN TREE CONTAINER FOR " + name);

        tree = treeContainer.GenerateTree(this);
        waitTime = Random.Range(0.1f, 1f);
        waitForSeconds = new WaitForSeconds(waitTime);
        StartCoroutine(Behave());
    }

    protected virtual IEnumerator Behave()
    {
        while (true)
        {
            treeStatus = tree.Process();
            yield return waitForSeconds;
        }
    }
    public void SetTreeContainer(BehaviourTreeContainer container)
    {
        treeContainer = container;
    }

    // Behaviour Tree friendly methods example (safe to remove)
    public NodeBT.Status IsPredatorNearby()
    {
        return NodeBT.Status.SUCCESS;
    }
    public NodeBT.Status RunAway()
    {
        return NodeBT.Status.SUCCESS;
    }
    protected NodeBT.Status ChooseRandomDirection()
    {
        return NodeBT.Status.SUCCESS;
    }
    public NodeBT.Status Walk()
    {
        return NodeBT.Status.SUCCESS;
    }
    public NodeBT.Status EatGrass()
    {
        return NodeBT.Status.FAILURE;
    }
}