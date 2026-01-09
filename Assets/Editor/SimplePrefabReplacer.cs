using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace WildernessSurvival.Editor
{
    public class SimplePrefabReplacer : EditorWindow
    {
        private GameObject source;
        private GameObject target;
        private bool copyScale = true;

        [MenuItem("Tools/Prefab Replacer")]
        public static void ShowWindow()
        {
            GetWindow<SimplePrefabReplacer>("Prefab Replacer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Prefab Replacer Settings", EditorStyles.boldLabel);

            source = (GameObject)EditorGUILayout.ObjectField("Source (Old)", source, typeof(GameObject), true);
            target = (GameObject)EditorGUILayout.ObjectField("Target (New Prefab)", target, typeof(GameObject), false);
            copyScale = EditorGUILayout.Toggle("Copy Scale", copyScale);

            EditorGUILayout.Space();

            if (GUILayout.Button("Replace All in Scene", GUILayout.Height(40)))
            {
                ReplaceAll();
            }

            if (source == null || target == null)
            {
                EditorGUILayout.HelpBox("Please assign both Source and Target objects.", MessageType.Warning);
            }
        }

        private void ReplaceAll()
        {
            if (source == null || target == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both Source and Target objects.", "OK");
                return;
            }

            // Trova tutti gli oggetti in scena. 
            // NOTA: Includiamo gli inattivi per essere sicuri di trovare tutto.
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<GameObject> objectsToReplace = new List<GameObject>();

            foreach (var obj in allObjects)
            {
                // Se l'oggetto è un'istanza del source (o è il source stesso se piazzato in scena)
                // Usiamo PrefabUtility per capire se appartiene allo stesso asset se il source è un prefab asset
                if (IsSourceMatch(obj))
                {
                    objectsToReplace.Add(obj);
                }
            }

            if (objectsToReplace.Count == 0)
            {
                EditorUtility.DisplayDialog("Result", "No instances of the source object found in the scene.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Confirm Replacement", $"Found {objectsToReplace.Count} objects to replace. Proceed?", "Yes", "Cancel"))
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Replace Prefabs");

            int count = 0;
            foreach (var oldObj in objectsToReplace)
            {
                Transform oldTransform = oldObj.transform;
                Vector3 pos = oldTransform.position;
                Quaternion rot = oldTransform.rotation;
                Vector3 scale = oldTransform.localScale;
                Transform parent = oldTransform.parent;

                // Crea il nuovo oggetto
                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(target);
                if (newObj == null)
                {
                    // Fallback se il target non è un prefab
                    newObj = Instantiate(target);
                }

                Undo.RegisterCreatedObjectUndo(newObj, "Create new prefab instance");
                
                newObj.transform.SetParent(parent);
                newObj.transform.SetPositionAndRotation(pos, rot);
                if (copyScale)
                {
                    newObj.transform.localScale = scale;
                }

                newObj.name = target.name;

                // Distruggi il vecchio oggetto
                Undo.DestroyObjectImmediate(oldObj);
                count++;
            }

            Undo.CollapseUndoOperations(groupIndex);

            Debug.Log($"[SimplePrefabReplacer] Successfully replaced {count} objects.");
            EditorUtility.DisplayDialog("Success", $"Successfully replaced {count} objects.", "OK");
        }

        private bool IsSourceMatch(GameObject obj)
        {
            if (obj == source) return true;

            // Se il source è un prefab asset, controlliamo se obj è un'istanza di quel prefab
            if (PrefabUtility.IsPartOfPrefabAsset(source))
            {
                GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                if (prefabRoot != null)
                {
                    GameObject sourceAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
                    if (sourceAsset == source) return true;
                }
            }
            
            // Fallback: se il nome coincide (opzionale, ma utile se i collegamenti prefab sono rotti)
            // if (obj.name == source.name) return true;

            return false;
        }
    }
}
