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
        private Label _pathErrorLabel;
        private ObjectField _targetBoneObjectField;
        private TextField _targetBonePathTextField;

        private void SetObjectField(string path, VRCAvatarDescriptor avatar)
        {
            var armature = avatar.transform.FindArmature();

            if (armature == null)
            {
                _pathErrorLabel.text = LL(en: "Failed to find the armature.", ko: "아바타의 Armature를 찾지 못했습니다.");
                return;
            }

            _targetBoneObjectField.style.display = DisplayStyle.Flex;
            _targetBonePathTextField.style.display = DisplayStyle.None;
            if (path != null && path.Length > 0)
            {
                var bone = armature.GetByPath(path);
                if (bone == null)
                {
                    _pathErrorLabel.style.display = DisplayStyle.Flex;
                    _pathErrorLabel.text = LL(
                        $"다음 경로의 본을 찾지 못했습니다: {path}",
                        $"Failed to find the bone at the following path: {path}"
                    );
                }
                else
                {
                    _targetBoneObjectField.SetValueWithoutNotify(bone);
                }
            }
        }

        private void SetTextField(string path)
        {
            _targetBonePathTextField.style.display = DisplayStyle.Flex;
            _targetBoneObjectField.style.display = DisplayStyle.None;
            if (path != null && path.Length > 0) _targetBonePathTextField.SetValueWithoutNotify(path);
        }

        private void UpdateEditorFields()
        {
            _targetBoneObjectField.style.display = DisplayStyle.None;
            _targetBonePathTextField.style.display = DisplayStyle.None;
            _pathErrorLabel.style.display = DisplayStyle.None;

            _targetBoneObjectField.SetValueWithoutNotify(null);
            _targetBonePathTextField.SetValueWithoutNotify("");

            var setParent = target as SetParent;
            if (setParent == null) return;

            var avatar = setParent.transform.FindAvatar();
            if (avatar == null) SetTextField(setParent._path);
            else SetObjectField(setParent._path, avatar);
        }

        public override void SetupInspector()
        {
            CloneTreeFromResource("SetParentEditor");
            var descriptionLabel = Inspector.Q<Label>("DescriptionLabel");
            L(
                "이 컴포넌트가 있는 GameObject는 시착 시 지정된 본의 자식으로 설정됩니다.",
                "The GameObject with this component will be set as a child of the specified bone when user try on.",
                s => { descriptionLabel.text = s; }
            );

            _targetBoneObjectField = Inspector.Q<ObjectField>("TargetBoneObjectField");
            _targetBonePathTextField = Inspector.Q<TextField>("TargetBonePathTextField");
            _pathErrorLabel = Inspector.Q<Label>("PathErrorLabel");

            UpdateEditorFields();

            EditorApplication.hierarchyChanged += Callback;
            _targetBoneObjectField.RegisterValueChangedCallback(e =>
            {
                var setParent = target as SetParent;
                if (setParent == null) return;

                var bone = (Transform) e.newValue;
                if (bone == null)
                {
                    serializedObject.FindProperty(nameof(setParent._path)).stringValue = "";
                    serializedObject.ApplyModifiedProperties();
                    UpdateEditorFields();
                    return;
                }

                var avatar = setParent.transform.FindAvatar();
                if (avatar == null) return;
                var armature = avatar.transform.FindArmature();
                if (armature == null) return;
                if (!bone.IsChildOf(armature))
                {
                    EditorUtility.DisplayDialog("Error", LL("본은 반드시 아바타의 Armature의 자식이어야 합니다.",
                            "The bone must be a child of the armature."),
                        "OK");

                    _targetBoneObjectField.SetValueWithoutNotify(e.previousValue);
                    return;
                }

                var path = armature.PathOf(bone);
                if (path == null) return;
                serializedObject.FindProperty(nameof(setParent._path)).stringValue = path;
                serializedObject.ApplyModifiedProperties();
                UpdateEditorFields();
            });


            var labelChanged = false;
            _targetBonePathTextField.RegisterValueChangedCallback(e =>
            {
                if (labelChanged)
                {
                    labelChanged = false;
                    UpdateEditorFields();
                    return;
                }

                var setParent = target as SetParent;
                if (setParent == null) return;
                serializedObject.FindProperty(nameof(setParent._path)).stringValue = e.newValue;
                serializedObject.ApplyModifiedProperties();
                UpdateEditorFields();
            });

            L(
                en: "Target Bone",
                ko: "대상 본",
                setter: s => { _targetBoneObjectField.label = s; }
            );

            L(
                en: "Target Bone Path",
                ko: "대상 본 경로",
                setter: s =>
                {
                    labelChanged = true;
                    _targetBonePathTextField.label = s;
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