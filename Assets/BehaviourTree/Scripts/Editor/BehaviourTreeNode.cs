using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AleM.BehaviourTrees
{
    public class BehaviourTreeNode : Node
    {
        public string GUID;

        public string methodName;
        public NodeBT.Type type;
        public bool random = false;
        public bool loopThroughAll = false;
        public bool inverted = false;

        public bool entryPoint = false;
        public int portCount;

        public delegate void DoubleClick();
        public DoubleClick OnDoubleClick;

        public delegate void ContentChanged();
        public ContentChanged OnContentChanged;

        private static readonly Color greyColor = new Color(0.28f, 0.28f, 0.28f, 0.5f);
        private static readonly Color invertedColor = new Color(0.5f, 0f, 0f, 0.5f);
        private static readonly Color orderedColor = new Color(0f, 0.5f, 0f, 0.5f);

        public BehaviourTreeNode()
        {
            titleContainer.style.unityFontStyleAndWeight = FontStyle.Bold;
        }
        public void SetupDoubleClick()
        {
            this.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }
        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == (int)MouseButton.LeftMouse)
            {
                ClickedTwice();
            }
        }
        private void ClickedTwice()
        {
            OnDoubleClick.Invoke();
        }
        public bool CanAddPorts()
        {
            if (type == NodeBT.Type.Leaf) return false;
            return true;
        }
        public void CreateCycleChildrenToggle()
        {
            // Cycle Children Toggle
            Toggle loopThroughAllToggle = new Toggle("Cycle Children");
            loopThroughAllToggle.value = loopThroughAll;
            loopThroughAllToggle.RegisterValueChangedCallback(evt =>
            {
                this.loopThroughAll = loopThroughAllToggle.value;
            });
            loopThroughAllToggle.tooltip = "Should this node call all children at once, or wait for the next agent update?";
            loopThroughAllToggle.style.backgroundColor = greyColor;
            loopThroughAllToggle.style.alignSelf = Align.FlexStart;
            this.mainContainer.Add(loopThroughAllToggle);
        }
        public void CreateRandomToggle()
        {
            Toggle randomToggle = new Toggle("Random?");
            randomToggle.value = random;
            randomToggle.RegisterValueChangedCallback(evt =>
            {
                this.random = randomToggle.value;
            });
            randomToggle.tooltip = "Call children randomly?";
            randomToggle.style.backgroundColor = greyColor;
            randomToggle.style.alignSelf = Align.FlexStart;
            this.mainContainer.Add(randomToggle);
        }
        public void SetupNodeStyle(string tooltipText, string sheetName)
        {
            this.style.backgroundColor = greyColor;
            this.tooltip = tooltipText;
            this.styleSheets.Add(Resources.Load<StyleSheet>(sheetName));
        }
        public void CreateInvertButton()
        {
            Button invertButton = new Button(() =>
            {
                this.inverted = !this.inverted;
                PortBT inputPort = this.inputContainer[0] as PortBT;
                if (inputPort.connectedEdge != null)
                {
                    PortBT outputPort = inputPort.connectedEdge.output as PortBT;
                    if (outputPort != null) outputPort.UpdatePortName();
                }
            });
            invertButton.text = this.inverted ? "Inverted" : "Ordered";
            invertButton.tooltip = this.inverted ? "Returns inverted status (!=)" : "Returns correct status (==)";
            invertButton.style.backgroundColor = this.inverted ? invertedColor : orderedColor;
            invertButton.clicked += () =>
            {
                invertButton.text = this.inverted ? "Inverted" : "Ordered";
                invertButton.tooltip = this.inverted ? "Returns inverted status (!=)" : "Returns correct status (==)";
                invertButton.style.backgroundColor = this.inverted ? invertedColor : orderedColor;
            };
            this.titleContainer.Add(invertButton);
        }
        public PortBT InstantiatePortBT(Orientation orientation, Direction direction, Port.Capacity capacity, System.Type type, int index)
        {
            return PortBT.CreateBT<Edge>(orientation, direction, capacity, type, index);
        }
    }
}