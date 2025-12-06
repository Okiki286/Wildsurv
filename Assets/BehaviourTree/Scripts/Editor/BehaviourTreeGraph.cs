using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AleM.BehaviourTrees
{
    public class BehaviourTreeGraph : EditorWindow
    {
        private BehaviourTreeGraphView graphView;

        private ObjectField selectedObjectField;
        private BTAgent selectedAgent;

        [MenuItem("Tools/Behaviour Tree Graph")]
        public static void OpenBehaviourTreeGraph()
        {
            var window = GetWindow<BehaviourTreeGraph>();
            window.titleContent = new GUIContent("Behaviour Tree Graph");
        }

        private void OnEnable()
        {
            selectedAgent = FindObjectsByType<BTAgent>(FindObjectsSortMode.None).FirstOrDefault(agent => agent.gameObject.GetInstanceID() == EditorPrefs.GetInt("BehaviourTree_SelectedAgent", -1));
            ConstructGraphView();
            GenerateToolbar();
            if (selectedAgent) UpdateGraph();
            GenerateMiniMap();
        }
        private void ConstructGraphView()
        {
            graphView = new BehaviourTreeGraphView
            {
                name = "Behaviour Tree Graph"
            };

            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
        }
        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            var saveButton = new Button(() =>
            {
                SaveGraph(selectedAgent);
            });
            var leafCreateButton = new Button(() =>
            {
                graphView.CreateNode("Leaf", NodeBT.Type.Leaf, null, null, selectedAgent, graphView.GetGraphViewCenter(), false);
            });
            var sequenceCreateButton = new Button(() =>
            {
                graphView.CreateNode("Sequence", NodeBT.Type.Sequence, null, null, selectedAgent, graphView.GetGraphViewCenter(), true);
            });
            var selectorCreateButton = new Button(() =>
            {
                graphView.CreateNode("Selector", NodeBT.Type.Selector, null, null, selectedAgent, graphView.GetGraphViewCenter(), true);
            });

            selectedObjectField = new ObjectField()
            {
                allowSceneObjects = true,
                objectType = typeof(BTAgent)
            };
            selectedObjectField.value = selectedAgent;

            selectedObjectField.RegisterValueChangedCallback<Object>((evt) =>
            {
                selectedAgent = (BTAgent)evt.newValue;

                if (selectedAgent)
                    EditorPrefs.SetInt("BehaviourTree_SelectedAgent", selectedAgent.gameObject.GetInstanceID());
                else
                    EditorPrefs.SetInt("BehaviourTree_SelectedAgent", -1);

                UpdateGraph();
            });

            Button setTreeContainerButton = new Button(() =>
            {
                if (selectedAgent == null) return;

                selectedAgent.SetTreeContainer(Resources.Load<BehaviourTreeContainer>(selectedAgent.name + "_btgraph"));
                EditorUtility.SetDirty(selectedAgent);
            });

            saveButton.text = "Save";
            leafCreateButton.text = "Create Leaf";
            sequenceCreateButton.text = "Create Sequence";
            selectorCreateButton.text = "Create Selector";
            setTreeContainerButton.text = "Set Container for Agent";

            toolbar.Add(saveButton);
            toolbar.Add(leafCreateButton);
            toolbar.Add(sequenceCreateButton);
            toolbar.Add(selectorCreateButton);
            toolbar.Add(selectedObjectField);
            toolbar.Add(setTreeContainerButton);

            rootVisualElement.Add(toolbar);
        }
        private void GenerateMiniMap()
        {
            var miniMap = new MiniMap
            {
                anchored = true
            };
            miniMap.SetPosition(new Rect(10, 30, 200, 140));
            graphView.Add(miniMap);
        }
        private void SaveGraph(BTAgent agent)
        {
            if (!agent) return;

            var saveUtility = GraphSaveUtility.GetInstance(graphView);
            saveUtility.SaveGraph(agent.name + "_btgraph");

            selectedAgent.SetTreeContainer(Resources.Load<BehaviourTreeContainer>(selectedAgent.name + "_btgraph"));
            EditorUtility.SetDirty(selectedAgent);
        }
        private void UpdateGraph()
        {
            if (!selectedAgent)
            {
                graphView.ClearGraph();
                return;
            }

            graphView.UpdateGraph(selectedAgent);
        }
        private void OnDisable()
        {
            rootVisualElement.Remove(graphView);
        }
    }
}