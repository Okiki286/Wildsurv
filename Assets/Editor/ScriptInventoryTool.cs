using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;

#if UNITY_EDITOR
namespace MyGame.EditorTools
{
    public class ScriptInventoryTool : EditorWindow
    {
        [MenuItem("Tools/MyGame/List Project Scripts")]
        public static void ListScripts()
        {
            // Percorso della cartella Assets
            string path = Application.dataPath;

            // Trova tutti i file .cs ricorsivamente
            string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Trovati {files.Length} script nel progetto:");
            sb.AppendLine("--------------------------------------------------");

            foreach (string file in files)
            {
                // Ottieni percorso relativo per leggibilità
                string relativePath = "Assets" + file.Replace(Application.dataPath, "").Replace('\\', '/');

                // Ignora file in cartelle esterne o package se non necessario
                if (relativePath.Contains("/Plugins/") || relativePath.Contains("/Editor/")) continue;

                FileInfo fi = new FileInfo(file);
                sb.AppendLine($"[FILE] {relativePath} ({fi.Length} bytes)");
            }

            Debug.Log(sb.ToString());
        }
    }
}
#endif