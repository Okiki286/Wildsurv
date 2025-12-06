using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace AleM.BehaviourTrees
{
    public class BehaviourTreeGraphView : GraphView
    {
        public override EventPropagation DeleteSelection()
        {
            return base.DeleteSelection();
        }
        private readonly Vector2 nodeSize = new Vector2(150, 200);

        private readonly Color newChildColor = new Color(0f, 0.3f, 0.3f, 1f);

        public BehaviourTreeGraphView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("BehaviourTreeGraph"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            SetupGrid();
        }
        private void SetupGrid()
        {
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ForEach((port) =>
            {
                if (startPort != port && startPort.node != port.node)
                    compatiblePorts.Add(port);
            });
            return compatiblePorts;
        }
        private PortBT GeneratePort(BehaviourTreeNode node, Direction direction, Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePortBT(Orientation.Horizontal, direction, capacity, typeof(bool), direction == Direction.Output ? node.portCount++ : 0);
        }
        public BehaviourTreeNode CreateNode(string nodeName, NodeBT.Type t, string guid, string methodName, BTAgent agent, Vector2 position, bool createOutputPort)
        {
            var node = CreateBehaviourTreeNode(nodeName, t, guid, methodName, agent, position, createOutputPort);
            AddElement(node);
            return node;
        }
        public BehaviourTreeNode CreateBehaviourTreeNode(string name, NodeBT.Type t, string guid,
            string methodName, BTAgent agent, Vector2 position, bool createOutputPort = false,
            bool loopThroughAll = false, bool random = false, bool inverted = false)
        {
            var btNode = new BehaviourTreeNode
            {
                title = name,
                type = t,
                methodName = methodName,
                inverted = inverted,
                random = random,
                loopThroughAll = loopThroughAll
            };
            //btNode.capabilities &= ~Capabilities.Deletable;
            switch (t)
            {
                case NodeBT.Type.Sequence:
                    {
                        // Cycle Children Toggle
                        btNode.CreateCycleChildrenToggle();

                        // Base Sequence
                        btNode.SetupNodeStyle(
                            "Calls children nodes until a child node returns FAILURE",
                            "SequenceNode");
                        break;
                    }
                case NodeBT.Type.Selector:
                    {
                        // Cycle Children Toggle
                        btNode.CreateCycleChildrenToggle();

                        // Random Selector Toggle
                        btNode.CreateRandomToggle();

                        // Base Selector
                        btNode.SetupNodeStyle(
                            "Calls children nodes until a child node returns SUCCESS",
                            "SelectorNode");
                        break;
                    }
                case NodeBT.Type.Leaf:
                    {
                        if (!agent) break;

                        // Get all method names
                        var methods = agent.GetType().GetMethods(
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic);

                        // Find BT method names
                        List<string> btMethods = new List<string>();
                        for (int i = 0; i < methods.Length; i++)
                        {
                            if (methods[i].ReturnType != typeof(BehaviourTrees.NodeBT.Status)) continue;
                            if (methods[i].GetParameters().Length > 0) continue;

                            btMethods.Add(methods[i].Name);
                        }

                        int defaultIndex = btMethods.IndexOf(methodName);

                        // Method Popup
                        PopupField<string> popup = new PopupField<string>("Select BT Method", btMethods, defaultIndex);
                        if (defaultIndex > -1 && !string.IsNullOrEmpty(popup.choices[defaultIndex])) btNode.title = Regex.Replace(popup.choices[defaultIndex], "(?<!^)([A-Z])", " $1"); ;

                        popup.RegisterValueChangedCallback((evt) =>
                        {
                            btNode.methodName = evt.newValue;
                            if (!string.IsNullOrEmpty(evt.newValue)) btNode.title = Regex.Replace(evt.newValue, "(?<!^)([A-Z])", " $1"); ;
                            if (btNode.OnContentChanged != null) btNode.OnContentChanged.Invoke();
                        });
                        btNode.outputContainer.Add(popup);

                        // Double click on node to open script
                        btNode.SetupDoubleClick();
                        btNode.OnDoubleClick += () =>
                        {
                            if (btNode.methodName == null) return;

                            MonoScript script = MonoScript.FromMonoBehaviour(agent);
                            string path = AssetDatabase.GetAssetPath(script);

                            int line = 0;
                            string[] lines = File.ReadAllLines(path);

                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (Regex.IsMatch(lines[i], @"\b" + methodName + @"\b\s*\("))
                                {
                                    line = i + 1;
                                    break;
                                }
                            }

                            InternalEditorUtility.OpenFileAtLineExternal(path, line);
                        };

                        btNode.tooltip = "Calls a method that returns SUCCESS, WORKING, or FAILURE";
                        btNode.styleSheets.Add(Resources.Load<StyleSheet>("LeafNode"));
                        break;
                    }
                default:
                    {
                        btNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));
                        break;
                    }
            }
            if (guid != null) btNode.GUID = guid;
            else btNode.GUID = Guid.NewGuid().ToString();

            // Output Port if Needed
            if (createOutputPort)
            {
                AddChildPort(btNode);
            }

            // Input Port
            var inputPort = GeneratePort(btNode, Direction.Input, Port.Capacity.Single);
            inputPort.portName = "In";
            btNode.inputContainer.Add(inputPort);

            // Invert Button
            btNode.CreateInvertButton();

            // New Child Button if Allowed
            if (btNode.CanAddPorts())
            {
                var button = new Button(() =>
                {
                    AddChildPort(btNode);
                });
                button.text = "New Child";
                button.tooltip = "Add new port to this node";
                button.style.backgroundColor = newChildColor;
                btNode.titleContainer.Add(button);
            }

            // Remove collapse button from node        
            VisualElement collapseButton = btNode.Q("collapse-button");
            if (collapseButton != null) btNode.titleButtonContainer.Remove(collapseButton);

            // Refresh and Set position
            btNode.RefreshExpandedState();
            btNode.RefreshPorts();
            btNode.SetPosition(new Rect(position, nodeSize));

            return btNode;
        }
        private BehaviourTreeNode GenerateEntryPointNode(string entryNodeName)
        {
            var node = new BehaviourTreeNode
            {
                title = entryNodeName,
                GUID = Guid.NewGuid().ToString(),
                type = NodeBT.Type.Tree,
                entryPoint = true
            };

            PortBT port = GeneratePort(node, Direction.Output);
            port.portName = "Start";
            node.outputContainer.Add(port);

            node.tooltip = "Start Node";
            node.capabilities &= ~Capabilities.Movable;
            node.capabilities &= ~Capabilities.Deletable;

            node.RefreshExpandedState();
            node.RefreshPorts();

            node.SetPosition(new Rect(100, 200, 100, 150));

            return node;
        }
        private void AddChildPort(BehaviourTreeNode node, string portName = null)
        {
            var port = GeneratePort(node, Direction.Output);

            port.contentContainer.Q<Label>("type").style.display = DisplayStyle.None;

            var outputPortCount = node.outputContainer.Query("connector").ToList().Count;
            string childPortName = portName != null ? portName : $"Child {outputPortCount}";

            port.nameLabel = new Label(childPortName);
            port.nameLabel.style.minWidth = 150;

            port.contentContainer.Add(new Label("  "));
            port.contentContainer.Add(port.nameLabel);

            Button removeButton = new Button(() => RemovePort(node, port))
            {
                text = "X"
            };
            port.contentContainer.Add(removeButton);

            port.portName = childPortName;
            node.outputContainer.Add(port);
            node.RefreshExpandedState();
            node.RefreshPorts();
        }
        private void RemovePort(BehaviourTreeNode node, PortBT port)
        {
            var targetEdge = edges.ToList().Where(x => x.output.portName == port.portName &&
            x.output.node == port.node);

            foreach (var e in targetEdge)
            {
                Debug.Log(e.output.portName);
            }
            if (targetEdge.Any())
            {
                Edge edge = targetEdge.First();
                edge.input.Disconnect(edge);
                RemoveElement(targetEdge.First());
            }
            node.portCount--;

            node.outputContainer.Remove(port);
            node.RefreshExpandedState();
            node.RefreshPorts();

        }
        public void ClearGraph()
        {
            nodes.ForEach((n) =>
            {
                RemoveElement(n);
            });
            edges.ForEach((e) =>
            {
                RemoveElement(e);
            });
        }
        public void UpdateGraph(BTAgent agent)
        {
            ClearGraph();
            bool containerLoaded = false;
            BehaviourTreeContainer container = Resources.Load<BehaviourTreeContainer>(agent.name + "_btgraph");
            if (container == null)
            {
                container = ScriptableObject.CreateInstance<BehaviourTreeContainer>();
            }
            else
            {
                containerLoaded = true;
            }

            BehaviourTreeNode entryNode = GenerateEntryPointNode(agent.name + " BT");
            AddElement(entryNode);

            if (containerLoaded) entryNode.GUID = container.entryNodeGUID;

            foreach (BTNodeData nodeData in container.nodeData)
            {
                if (nodeData == null) continue;

                var tempNode = CreateBehaviourTreeNode(nodeData.name, nodeData.type,
                    nodeData.GUID, nodeData.leafMethod, agent, nodeData.position, false,
                    nodeData.loopThroughAll, nodeData.random, nodeData.inverted);
                AddElement(tempNode);

                var nodePorts = container.nodeLinks.Where(x => x.baseNodeGUID == nodeData.GUID).ToList();
                nodePorts.ForEach(x => AddChildPort(tempNode, x.portName));
            }

            List<BehaviourTreeNode> Nodes = nodes.ToList().Cast<BehaviourTreeNode>().ToList();

            for (int i = 0; i < Nodes.Count; i++)
            {
                List<BTNodeLinkData> connections = container.nodeLinks.Where(x => x.baseNodeGUID == Nodes[i].GUID).ToList();
                List<BTNodeLinkData> tempList = new List<BTNodeLinkData>();

                while (tempList.Count != connections.Count)
                {
                    int lowestIndex = 10000;
                    BTNodeLinkData data = connections[0];
                    for (int b = 0; b < connections.Count; b++)
                    {
                        if (tempList.Contains(connections[b])) continue;

                        if (connections[b].outputPortIndex < lowestIndex)
                        {
                            lowestIndex = connections[b].outputPortIndex;
                            data = connections[b];
                        }
                    }
                    tempList.Add(data);
                }
                for (int j = 0; j < tempList.Count; j++)
                {
                    string targetNodeGuid = tempList[j].targetNodeGUID;
                    BehaviourTreeNode targetNode = Nodes.First(x => x.GUID == targetNodeGuid);
                    LinkNodes(Nodes[i].outputContainer[j].Q<PortBT>(), (PortBT)targetNode.inputContainer[0]);

                    targetNode.SetPosition(new Rect(
                        container.nodeData.First(x => x.GUID == targetNodeGuid).position,
                        nodeSize));
                }
            }
        }
        private void LinkNodes(PortBT output, PortBT input)
        {
            var edge = new Edge
            {
                output = output,
                input = input
            };
            edge.input.Connect(edge);
            edge.output.Connect(edge);
            Add(edge);
        }
        public Vector2 GetGraphViewCenter()
        {
            return (layout.center - (Vector2)viewTransform.position) / viewTransform.scale;
        }
    }
}