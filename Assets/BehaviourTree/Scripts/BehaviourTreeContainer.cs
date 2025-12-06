using System;
using System.Collections.Generic;
using UnityEngine;

namespace AleM.BehaviourTrees
{
    [System.Serializable]
    public class BehaviourTreeContainer : ScriptableObject
    {
        public string entryNodeGUID;

        public List<BTNodeData> nodeData = new List<BTNodeData>();
        public List<BTNodeLinkData> nodeLinks = new List<BTNodeLinkData>();

        public BehaviourTree GenerateTree(BTAgent agent)
        {
            BehaviourTree tree = new BehaviourTree("Behaviour Tree");
            if (nodeData == null)
            {
                Debug.LogError($" {agent.name} Container has corrupted node data");
                return tree;
            }

            Dictionary<BTNodeData, NodeBT> nodes = new Dictionary<BTNodeData, NodeBT>();
            foreach (BTNodeData data in nodeData)
            {
                switch (data.type)
                {
                    case NodeBT.Type.Sequence:
                        {
                            Sequence sequence = new Sequence(data.name, data.loopThroughAll);
                            if (data.inverted)
                            {
                                Inverter inverter = new Inverter("Invert " + data.name);
                                inverter.AddChild(sequence);
                                nodes.Add(data, inverter);
                                break;
                            }
                            nodes.Add(data, sequence);
                            break;
                        }
                    case NodeBT.Type.Selector:
                        {
                            if (data.inverted)
                            {
                                Inverter inverter = new Inverter("Invert " + data.name);
                                inverter.AddChild(data.random ? new RSelector(data.name, data.loopThroughAll) : new Selector(data.name, data.loopThroughAll));
                                nodes.Add(data, inverter);
                                break;
                            }
                            nodes.Add(data, data.random ? new RSelector(data.name, data.loopThroughAll) : new Selector(data.name, data.loopThroughAll));
                            break;
                        }
                    case NodeBT.Type.Leaf:
                        {
                            var methodInfo = agent.GetType().GetMethod(data.leafMethod,
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic);
                            Leaf.Tick leafAction = (Leaf.Tick)Delegate.CreateDelegate(typeof(Leaf.Tick), agent, methodInfo.Name);

                            Leaf leaf = new Leaf(data.name, leafAction);
                            if (data.inverted)
                            {
                                Inverter inverter = new Inverter("Invert " + data.name);
                                inverter.AddChild(leaf);
                                nodes.Add(data, inverter);
                                break;
                            }
                            nodes.Add(data, leaf);
                            break;
                        }
                }
            }
            for (int i = 1; i < nodeLinks.Count; i++)
            {
                if (nodeLinks[i] == null) continue;

                if (!nodes.TryGetValue(nodeData.Find(x => x.GUID == nodeLinks[i].baseNodeGUID), out NodeBT outputNode)) return null;
                if (!nodes.TryGetValue(nodeData.Find(x => x.GUID == nodeLinks[i].targetNodeGUID), out NodeBT inputNode)) return null;

                outputNode.AddChild(inputNode);
            }
            nodes.TryGetValue(nodeData.Find(x => x.GUID == nodeLinks[0].targetNodeGUID), out NodeBT firstNode);
            tree.AddChild(firstNode);
            return tree;

        }
    }
    [System.Serializable]
    public class BTNodeData
    {
        public string GUID;
        public string name;
        public NodeBT.Type type;
        public bool random;
        public bool loopThroughAll;
        public bool inverted;
        public Vector2 position;
        public string leafMethod;
    }
    [System.Serializable]
    public class BTNodeLinkData
    {
        public string baseNodeGUID;
        public int outputPortIndex;
        public string portName;
        public string targetNodeGUID;
        public int inputPortIndex;
    }
}
