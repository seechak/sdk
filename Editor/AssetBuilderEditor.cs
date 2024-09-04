using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SEECHAK.SDK.Editor.Core;
using Unity.VisualScripting.IonicZip;
using UnityEditor;
using UnityEngine;

namespace SEECHAK.SDK.Editor
{
    public class AssetBuilderEditor : EditorWindow
    {
        [MenuItem("SEECHAK/Build Asset")]
        public static void ShowWindow()
        {
            GetWindow<AssetBuilderEditor>("Build Asset");
        }

        private void OnGUI()
        {
            var objects = Selection.gameObjects;

            if (objects.Length == 0)
            {
                EditorGUILayout.HelpBox("Select a game object(s) to build", MessageType.Info);
            }
            else
            {
                var style = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 10,
                    margin = new RectOffset(0, 0, 0, 0),
                };
                foreach (var obj in objects)
                {
                    EditorGUILayout.LabelField(obj.name, style);
                }
            }

            if (GUILayout.Button("Build & Save"))
            {
                if (objects.Length == 0)
                {
                    EditorUtility.DisplayDialog("Error", "Select object(s) to build", "OK");
                    return;
                }

                UniTask.Void(async () => await BuildAndSave(objects));
            }

            Repaint();
        }

        private async Task BuildAndSave(GameObject[] objects)
        {
            var path = EditorUtility.SaveFilePanel("Save Asset", Application.dataPath, "Assets", "zip");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var tempFilePath = Temp.GetPath("Asset.zip");
            using var archive = new ZipFile(tempFilePath);

            foreach (var obj in objects)
            {
                var result = AssetBuilder.Build(obj);
                var assetBytes = await File.ReadAllBytesAsync(result.Path);
                archive.AddEntry(obj.name, assetBytes);
            }

            archive.Save();
            File.Delete(path);
            File.Move(tempFilePath, path);
            EditorUtility.DisplayDialog("Success", "Asset has been saved", "OK");
        }
    }

}