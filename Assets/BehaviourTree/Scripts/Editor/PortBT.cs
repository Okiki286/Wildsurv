using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AleM.BehaviourTrees
{
    public class PortBT : Port
    {
        private int index;
        public int Index => index;

        public Edge connectedEdge;

        public Label nameLabel;
        protected PortBT(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {

        }

        public static PortBT CreateBT<TEdge>(Orientation orientation, Direction direction, Capacity capacity, Type type, int index) where TEdge : Edge, new()
        {
            DefaultEdgeConnectorListener listener = new DefaultEdgeConnectorListener();
            PortBT port = new PortBT(orientation, direction, capacity, type)
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(listener),
                index = index
            };
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }
        public override void Connect(Edge edge)
        {
            base.Connect(edge);

            connectedEdge = edge;
            if (nameLabel == null) return;

            UpdatePortName();
            ((BehaviourTreeNode)edge.input.node).OnContentChanged += () => UpdatePortName();
        }
        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);

            connectedEdge = null;
            if (nameLabel == null) return;

            UpdatePortName();
            ((BehaviourTreeNode)edge.input.node).OnContentChanged -= () => UpdatePortName();
        }
        public void UpdatePortName()
        {
            if (nameLabel == null) return;
            if (connectedEdge == null)
            {
                SetPortLabelName("Nothing");
            }
            else if (connectedEdge.input != this)
            {
                BehaviourTreeNode inputNode = connectedEdge.input.node as BehaviourTreeNode;
                string title = inputNode.title;
                if (inputNode.inverted)
                {
                    title += " (Inverted)";
                }
                SetPortLabelName(title);
            }
        }
        private void SetPortLabelName(string title)
        {
            string text = "";
            if (((BehaviourTreeNode)node).type == BehaviourTrees.NodeBT.Type.Sequence)
            {
                text += "Do: ";
            }
            else if (((BehaviourTreeNode)node).type == BehaviourTrees.NodeBT.Type.Selector)
            {
                text += index == 0 ? "Do: " : "Or: ";
            }
            nameLabel.text = text + title;
        }
        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            private GraphViewChange m_GraphViewChange;

            private List<Edge> m_EdgesToCreate;

            private List<GraphElement> m_EdgesToDelete;

            public DefaultEdgeConnectorListener()
            {
                m_EdgesToCreate = new List<Edge>();
                m_EdgesToDelete = new List<GraphElement>();
                m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                m_EdgesToCreate.Clear();
                m_EdgesToCreate.Add(edge);
                m_EdgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                {
                    foreach (Edge connection in edge.input.connections)
                    {
                        if (connection != edge)
                        {
                            m_EdgesToDelete.Add(connection);
                        }
                    }
                }

                if (edge.output.capacity == Capacity.Single)
                {
                    foreach (Edge connection2 in edge.output.connections)
                    {
                        if (connection2 != edge)
                        {
                            m_EdgesToDelete.Add(connection2);
                        }
                    }
                }

                if (m_EdgesToDelete.Count > 0)
                {
                    graphView.DeleteElements(m_EdgesToDelete);
                }

                List<Edge> edgesToCreate = m_EdgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
                }

                foreach (Edge item in edgesToCreate)
                {
                    graphView.AddElement(item);
                    edge.input.Connect(item);
                    edge.output.Connect(item);
                }
            }
        }
    }
}