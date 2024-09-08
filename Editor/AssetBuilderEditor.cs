using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SEECHAK.SDK.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace SEECHAK.SDK.Editor
{
    public class AssetBuilderEditor : EditorWindow
    {
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
                    margin = new RectOffset(0, 0, 0, 0)
                };
                foreach (var obj in objects) EditorGUILayout.LabelField(obj.name, style);
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

        [MenuItem("SEECHAK/Build Asset")]
        public static void ShowWindow()
        {
            GetWindow<AssetBuilderEditor>("Build Asset");
        }

        private async Task BuildAndSave(GameObject[] objects)
        {
            var path = EditorUtility.SaveFilePanel("Save Asset", Application.dataPath, "Assets", "zip");
            if (string.IsNullOrEmpty(path)) return;

            var tempFilePath = Temp.GetPath("Asset.zip");
            if (File.Exists(tempFilePath)) AssetDatabase.DeleteAsset(tempFilePath);
            using (var archive = ZipFile.Open(tempFilePath, ZipArchiveMode.Create))
            {
                foreach (var obj in objects)
                {
                    AssetBuilder.CleanUp();
                    var result = AssetBuilder.Build(obj);
                    var entry = archive.CreateEntry(obj.name);
                    await using var zipStream = entry.Open();
                    await using var fileStream = new FileStream(result.Path, FileMode.Open);
                    await fileStream.CopyToAsync(zipStream);
                }
            }

            AssetBuilder.CleanUp();
            File.Delete(path);
            File.Move(tempFilePath, path);
            EditorUtility.DisplayDialog("Success", "Asset has been saved", "OK");
        }
    }
}