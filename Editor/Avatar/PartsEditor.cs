using System;
using System.Collections.Generic;
using System.Linq;
using SEECHAK.SDK.Core.Avatar;
using UnityEditor;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

namespace SEECHAK.SDK.Editor.Avatar
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Parts))]
    public class PartsEditor : SeechakInspector
    {
        public override void SetupInspector()
        {
            CloneTreeFromResource("PartsEditor");

            var togglesVisualElement = Inspector.Q<VisualElement>("TogglesVisualElement");

            var kinds = new List<string> {"hair", "top", "bottom", "shoes", "accessory", "kemomimi", "tail"};
            var englishKinds = new List<string> {"Hair", "Top", "Bottom", "Shoes", "Accessory", "Kemomimi", "Tail"};
            var koreanKinds = new List<string> {"헤어", "상의", "하의", "신발", "액세서리", "케모미미", "꼬리"};
            for (var index = 0; index < kinds.Count; index++)
            {
                var kind = kinds[index];
                var toggle = new Toggle();
                L(
                    koreanKinds[index],
                    englishKinds[index],
                    s => toggle.text = s
                );
                toggle.style.marginTop = 0;
                toggle.style.marginBottom = 2;
                toggle.style.marginLeft = 0;
                toggle.style.marginRight = 0;
                togglesVisualElement.Add(toggle);

                var kindsValue = (target as Parts)?._kinds ?? Array.Empty<string>();
                toggle.value = kindsValue.Contains(kind);

                toggle.RegisterValueChangedCallback(e =>
                {
                    var kindsValue = (target as Parts)?._kinds ?? Array.Empty<string>();
                    kindsValue = e.newValue
                        ? kindsValue.Append(kind).ToArray()
                        : kindsValue.Where(k => k != kind).ToArray();

                    var kindsProperty = serializedObject.FindProperty(nameof(Parts._kinds));
                    kindsProperty.ClearArray();
                    foreach (var kindValue in kindsValue)
                    {
                        kindsProperty.InsertArrayElementAtIndex(kindsProperty.arraySize);
                        kindsProperty.GetArrayElementAtIndex(kindsProperty.arraySize - 1).stringValue = kindValue;
                    }

                    serializedObject.ApplyModifiedProperties();
                });
            }


            var descriptionLabel = Inspector.Q<Label>("DescriptionLabel");
            L(
                "시착 시 시착하려는 에셋의 착용 부위에 따라 적절히 비활성화 됩니다.",
                "It will be disabled appropriately depending on the part of the asset you want to wear.",
                s => { descriptionLabel.text = s; }
            );
        }
    }
}