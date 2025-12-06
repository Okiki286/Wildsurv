using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace AleM.BehaviourTrees
{
    public class GraphSaveUtility
    {
        private BehaviourTreeGraphView targetGraphView;

        private List<Edge> Edges
        {
            get
            {
                return targetGraphView.edges.ToList();
            }
        }
        private List<BehaviourTreeNode> Nodes => targetGraphView.nodes.ToList().Cast<BehaviourTreeNode>().ToList();

        public static GraphSaveUtility GetInstance(BehaviourTreeGraphView targetGraphView)
        {
            return new GraphSaveUtility
            {
                targetGraphView = targetGraphView,
            };
        }
        public void SaveGraph(string fileName)
        {
            if (!Edges.Any()) return;

            var behaviourTreeContainer = ScriptableObject.CreateInstance<BehaviourTreeContainer>();

            Edge[] connectedPorts = Edges.Where(x => x.input.node != null).ToArray();

            Dictionary<string, Dictionary<int, Edge>> edges = new Dictionary<string, Dictionary<int, Edge>>();
            for (int i = 0; i < connectedPorts.Length; i++)
            {
                Edge edge = connectedPorts[i];
                var outputNode = edge.output.node as BehaviourTreeNode;
                var inputNode = edge.input.node as BehaviourTreeNode;

                if (outputNode.entryPoint)
                {
                    behaviourTreeContainer.entryNodeGUID = outputNode.GUID;
                }

                Dictionary<int, Edge> conns;
                if (edges.ContainsKey(outputNode.GUID))
                {
                    conns = edges[outputNode.GUID];
                }
                else
                {
                    conns = new Dictionary<int, Edge>();
                }
                if (conns.ContainsKey(((PortBT)edge.output).Index))
                {
                    continue;
                }
                conns.Add(((PortBT)edge.output).Index, edge);
                if (edges.ContainsKey(outputNode.GUID))
                {
                    edges[outputNode.GUID] = conns;
                }
                else
                {
                    edges.Add(outputNode.GUID, conns);
                }
            }
            for (int i = 0; i < edges.Count; i++)
            {
                string baseNodeGUID = edges.Keys.ElementAt(i);
                for (int j = 0; j < edges[baseNodeGUID].Count; j++)
                {
                    int times = 0;
                    while (!edges[baseNodeGUID].ContainsKey(j))
                    {
                        j++;
                        if (++times > 25)
                        {
                            throw new System.Exception("FATAL ERROR WITH CONNECTED PORTS");
                        }
                    }
                    Edge edge = edges[baseNodeGUID][j];
                    j -= times;
                    var outputNode = edge.output.node as BehaviourTreeNode;
                    var inputNode = edge.input.node as BehaviourTreeNode;

                    behaviourTreeContainer.nodeLinks.Add(new BTNodeLinkData
                    {
                        baseNodeGUID = outputNode.GUID,
                        inputPortIndex = ((PortBT)edge.input).Index,
                        portName = edge.output.portName,
                        outputPortIndex = ((PortBT)edge.output).Index,
                        targetNodeGUID = inputNode.GUID
                    });
                }
            }

            foreach (var btNode in Nodes.Where(node => !node.entryPoint))
            {
                BTNodeData data = new BTNodeData
                {
                    GUID = btNode.GUID,
                    name = btNode.title,
                    random = btNode.random,
                    type = btNode.type,
                    position = btNode.GetPosition().position,
                    loopThroughAll = btNode.loopThroughAll,
                    inverted = btNode.inverted
                };

                if (btNode.type == BehaviourTrees.NodeBT.Type.Leaf)
                {
                    data.leafMethod = btNode.methodName;
                }

                behaviourTreeContainer.nodeData.Add(data);
            }
            //if (!AssetDatabase.IsValidFolder("Assets/BehaviourTree"))
            //    AssetDatabase.CreateFolder("Assets", "Resources");

            AssetDatabase.CreateAsset(behaviourTreeContainer, $"Assets/BehaviourTree/Resources/{fileName}.asset");
            AssetDatabase.SaveAssets();
        }
    }
}