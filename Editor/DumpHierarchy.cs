using SEECHAK.SDK.Editor.Core.API;
using UnityEditor;
using UnityEngine;

namespace SEECHAK.SDK
{
    public class DumpHierarchy
    {
        [MenuItem("GameObject/SEECHAK/Dump Hierarchy To Clipboard", false, 0)]
        public static void Dump()
        {
            var gameObject = Selection.activeGameObject;
            if (gameObject == null)
            {
                Debug.LogError("No game object selected");
                return;
            }

            var unityObject = UnityObject.Input.UnityObject.FromGameObject(gameObject);
            var json = Request.Serialize(unityObject.Hierarchy);
            GUIUtility.systemCopyBuffer = json;
        }
    }
}