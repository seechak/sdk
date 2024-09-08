using SEECHAK.SDK.Core;
using SEECHAK.SDK.Core.Asset;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;

namespace SEECHAK.SDK.Editor.Asset
{
    [CustomEditor(typeof(SetParent))]
    public class SetParentEditor : SeechakInspector
    {
        private ObjectField targetBoneObjectField;
        private TextField targetBonePathTextField;
        private Label pathErrorLabel;
        private SerializedProperty pathProperty;

        private void SetObjectField(string path, VRCAvatarDescriptor avatar)
        {
            var armature = avatar.transform.FindArmature();

            if (armature == null)
            {
                pathErrorLabel.text = LL(en: "Failed to find the armature.", ko: "아바타의 Armature를 찾지 못했습니다.");
                return;
            }
            targetBoneObjectField.style.display = DisplayStyle.Flex;
            targetBonePathTextField.style.display = DisplayStyle.None;
            if (path != null && path.Length > 0)
            {
                var bone = armature.GetByPath(path);
                if (bone == null)
                {
                    pathErrorLabel.style.display = DisplayStyle.Flex;
                    pathErrorLabel.text = LL(
                        ko: $"다음 경로의 본을 찾지 못했습니다: {path}",
                        en: $"Failed to find the bone at the following path: {path}"
                    );
                }
                else
                {
                    targetBoneObjectField.SetValueWithoutNotify(bone);
                }
            }
        }

        private void SetTextField(string path)
        {
            targetBonePathTextField.style.display = DisplayStyle.Flex;
            targetBoneObjectField.style.display = DisplayStyle.None;
            if (path != null && path.Length > 0)
            {
                targetBonePathTextField.SetValueWithoutNotify(path);
            }
        }

        private void UpdateEditorFields()
        {
            targetBoneObjectField.style.display = DisplayStyle.None;
            targetBonePathTextField.style.display = DisplayStyle.None;
            pathErrorLabel.style.display = DisplayStyle.None;

            targetBoneObjectField.SetValueWithoutNotify(null);
            targetBonePathTextField.SetValueWithoutNotify("");

            var avatar = target != null ? (target as SetParent)?.transform.FindAvatar() : null;
            var path = pathProperty.stringValue;
            if (avatar == null) SetTextField(path);
            else SetObjectField(path, avatar);
        }

        public override void SetupInspector()
        {
            CloneTreeFromResource("SetParentEditor");
            var descriptionLabel = Inspector.Q<Label>("DescriptionLabel");
            L(
                ko: "이 컴포넌트가 있는 GameObject는 시착 시 지정된 본의 자식으로 설정됩니다.",
                en: "The GameObject with this component will be set as a child of the specified bone when user try on.",
                setter: (s) =>
                {
                    descriptionLabel.text = s;
                }
            );

            targetBoneObjectField = Inspector.Q<ObjectField>("TargetBoneObjectField");
            targetBonePathTextField = Inspector.Q<TextField>("TargetBonePathTextField");
            pathErrorLabel = Inspector.Q<Label>("PathErrorLabel");

            pathProperty = serializedObject.FindProperty("_path");

            UpdateEditorFields();
            ;

            EditorApplication.hierarchyChanged += Callback;
            targetBoneObjectField.RegisterValueChangedCallback((e) =>
            {
                var bone = (Transform)e.newValue;
                if (bone == null)
                {
                    pathProperty.stringValue = "";
                    serializedObject.ApplyModifiedProperties();
                    UpdateEditorFields();
                    return;
                }

                if (target != null)
                {
                    var setParent = target as SetParent;
                    var avatar = setParent.transform.FindAvatar();
                    var armature = avatar.transform.FindArmature();
                    if (!bone.IsChildOf(armature))
                    {
                        EditorUtility.DisplayDialog("Error", LL(ko: "본은 반드시 아바타의 Armature의 자식이어야 합니다.",
                                                                en: "The bone must be a child of the armature."),
                                                                "OK");

                        targetBoneObjectField.SetValueWithoutNotify(e.previousValue);
                        return;
                    }

                    serializedObject.FindProperty("path").stringValue = armature.PathOf(bone);
                    serializedObject.ApplyModifiedProperties();
                    UpdateEditorFields();
                }
            });


            bool labelChanged = false;
            targetBonePathTextField.RegisterValueChangedCallback(e =>
            {
                if (labelChanged)
                {
                    labelChanged = false;
                    UpdateEditorFields();
                    return;
                }
                pathProperty.stringValue = e.newValue;
                serializedObject.ApplyModifiedProperties();
                UpdateEditorFields();
            });

            L(
                en: "Target Bone",
                ko: "대상 본",
                setter: (s) =>
                {
                    targetBoneObjectField.label = s;
                }
            );

            L(
                en: "Target Bone Path",
                ko: "대상 본 경로",
                setter: (s) =>
                {
                    labelChanged = true;
                    targetBonePathTextField.label = s;
                }
            );

            void Callback()
            {
                if (target == null)
                {
                    EditorApplication.hierarchyChanged -= Callback;
                    return;
                }
                UpdateEditorFields();
            }
        }
    }
}