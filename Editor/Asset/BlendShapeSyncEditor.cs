using System;
using System.Collections.Generic;
using System.Linq;
using SEECHAK.SDK.Core;
using SEECHAK.SDK.Core.Asset;
using SEECHAK.SDK.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;

namespace SEECHAK.SDK
{
    [CustomEditor(typeof(BlendShapeSync))]
    public class BlendShapeSyncEditor : SeechakInspector
    {
        private Label _pathErrorLabel;
        private ObjectField _sourceRendererObjectField;
        private TextField _sourceRendererPathTextField;
        private ListView _syncedBlendShapesListView;

        private void SetObjectField(string path, VRCAvatarDescriptor avatar)
        {
            _sourceRendererObjectField.style.display = DisplayStyle.Flex;
            _sourceRendererPathTextField.style.display = DisplayStyle.None;
            if (path != null && path.Length > 0)
            {
                var rendererTransform = avatar.transform.GetByPath(path);
                if (rendererTransform != null &&
                    rendererTransform.TryGetComponent<SkinnedMeshRenderer>(out var renderer))
                {
                    _sourceRendererObjectField.SetValueWithoutNotify(renderer);
                }
                else
                {
                    _pathErrorLabel.style.display = DisplayStyle.Flex;
                    _pathErrorLabel.text = LL(
                        $"다음 경로의 SkinnedMeshRenderer을 찾지 못했습니다: {path}",
                        $"SkinnedMeshRenderer at the following path could not be found: {path}"
                    );
                }
            }
        }

        private void SetTextField(string path)
        {
            _sourceRendererPathTextField.style.display = DisplayStyle.Flex;
            _sourceRendererObjectField.style.display = DisplayStyle.None;
            if (path != null && path.Length > 0) _sourceRendererPathTextField.SetValueWithoutNotify(path);
        }

        private void UpdateEditorFields()
        {
            _sourceRendererObjectField.style.display = DisplayStyle.None;
            _sourceRendererPathTextField.style.display = DisplayStyle.None;
            _pathErrorLabel.style.display = DisplayStyle.None;

            _sourceRendererObjectField.SetValueWithoutNotify(null);
            _sourceRendererPathTextField.SetValueWithoutNotify("");

            var blendShapeSync = target as BlendShapeSync;
            if (blendShapeSync == null) return;
            var avatar = target != null ? blendShapeSync.transform.FindAvatar() : null;

            if (avatar == null)
                SetTextField(blendShapeSync._path);
            else
                SetObjectField(blendShapeSync._path, avatar);
        }

        private List<string> GetBlendShapeNames(SkinnedMeshRenderer renderer)
        {
            var blendShapeNames = new List<string>();
            for (var i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                blendShapeNames.Add(renderer.sharedMesh.GetBlendShapeName(i));

            return blendShapeNames;
        }

        public override void SetupInspector()
        {
            var blendShapeSync = target as BlendShapeSync;
            if (blendShapeSync == null) return;

            CloneTreeFromResource("BlendShapeSyncEditor");
            var descriptionLabel = Inspector.Q<Label>("DescriptionLabel");
            L(
                "소스 SkinnedMeshRenderer의 BlendShape 값을 이 컴포넌트가 부착된 오브젝트의 SkinnedMeshRenderer에 동기화합니다.",
                "Synchronizes the BlendShape values of the source SkinnedMeshRenderer to the SkinnedMeshRenderer of the object this component is attached to.",
                s => { descriptionLabel.text = s; }
            );

            _sourceRendererObjectField = Inspector.Q<ObjectField>("SourceRendererObjectField");
            _sourceRendererPathTextField = Inspector.Q<TextField>("SourceRendererPathTextField");
            _pathErrorLabel = Inspector.Q<Label>("PathErrorLabel");
            _syncedBlendShapesListView = Inspector.Q<ListView>("SyncedBlendShapesListView");
            var sourceLabel = Inspector.Q<Label>("SourceLabel");
            var targetLabel = Inspector.Q<Label>("TargetLabel");
            var weightLabel = Inspector.Q<Label>("WeightLabel");

            _sourceRendererObjectField.objectType = typeof(SkinnedMeshRenderer);

            UpdateEditorFields();

            EditorApplication.hierarchyChanged += Callback;
            _sourceRendererObjectField.RegisterValueChangedCallback(e =>
            {
                var sourceRenderer = (SkinnedMeshRenderer) e.newValue;
                if (sourceRenderer == null)
                {
                    serializedObject.FindProperty(nameof(BlendShapeSync._path)).stringValue = "";
                    serializedObject.ApplyModifiedProperties();
                    _syncedBlendShapesListView.RefreshItems();
                    UpdateEditorFields();
                    return;
                }

                if (target == null) return;

                var blendShapeSync = target as BlendShapeSync;
                if (blendShapeSync == null) return;
                var avatar = blendShapeSync.transform.FindAvatar();
                if (avatar == null) return;
                if (!sourceRenderer.transform.IsChildOf(avatar.transform))
                {
                    EditorUtility.DisplayDialog("Error",
                        LL("SkinnedMeshRenderer가 아바타의 자식이 아닙니다.", "SkinnedMeshRenderer is not a child of the avatar."),
                        "OK");

                    _sourceRendererObjectField.SetValueWithoutNotify(e.previousValue);
                    return;
                }

                serializedObject.FindProperty(nameof(BlendShapeSync._path)).stringValue =
                    avatar.transform.PathOf(sourceRenderer.transform);
                serializedObject.ApplyModifiedProperties();

                _syncedBlendShapesListView.RefreshItems();
                UpdateEditorFields();
            });


            var labelChanged = false;
            _sourceRendererPathTextField.RegisterValueChangedCallback(e =>
            {
                if (labelChanged)
                {
                    labelChanged = false;
                    UpdateEditorFields();
                    return;
                }

                serializedObject.FindProperty(nameof(BlendShapeSync._path)).stringValue = e.newValue;
                serializedObject.ApplyModifiedProperties();
                UpdateEditorFields();
            });

            _syncedBlendShapesListView.makeItem = () =>
            {
                var asset = Resources.Load<VisualTreeAsset>("BlendShapeSyncEditorElement");
                return asset.CloneTree();
            };

            _syncedBlendShapesListView.bindItem = (element, i) =>
            {
                var blendShapesProperty = serializedObject.FindProperty(nameof(BlendShapeSync._blendShapes));
                if (i >= blendShapesProperty.arraySize)
                {
                    blendShapesProperty.InsertArrayElementAtIndex(i);
                    blendShapesProperty.GetArrayElementAtIndex(i)
                        .FindPropertyRelative(nameof(BlendShapeSync.BlendShape._weight)).floatValue = 1f;
                    serializedObject.ApplyModifiedProperties();
                }

                var elementProperty = blendShapesProperty.GetArrayElementAtIndex(i);
                var sourceProperty = elementProperty.FindPropertyRelative(nameof(BlendShapeSync.BlendShape._source));
                var targetProperty = elementProperty.FindPropertyRelative(nameof(BlendShapeSync.BlendShape._target));
                var weightProperty = elementProperty.FindPropertyRelative(nameof(BlendShapeSync.BlendShape._weight));

                var sourceDropdownField = element.Q<DropdownField>("SourceDropdownField");
                var targetDropdownField = element.Q<DropdownField>("TargetDropdownField");
                var sourceTextField = element.Q<TextField>("SourceTextField");
                var targetTextField = element.Q<TextField>("TargetTextField");
                var weightFloatField = element.Q<FloatField>("WeightFloatField");

                var blendShapeSync = target as BlendShapeSync;
                if (blendShapeSync == null) return;
                var avatar = blendShapeSync.transform.FindAvatar();

                sourceDropdownField.style.display = DisplayStyle.None;
                sourceTextField.style.display = DisplayStyle.None;
                targetDropdownField.style.display = DisplayStyle.None;
                targetTextField.style.display = DisplayStyle.None;

                var sourceRendererPath = serializedObject.FindProperty(nameof(BlendShapeSync._path)).stringValue;
                if (avatar?.transform.GetByPath(sourceRendererPath)
                        ?.TryGetComponent<SkinnedMeshRenderer>(out var sourceRenderer) == true)
                {
                    sourceDropdownField.choices = GetBlendShapeNames(sourceRenderer);
                    sourceDropdownField.style.display = DisplayStyle.Flex;
                }
                else
                {
                    sourceTextField.style.display = DisplayStyle.Flex;
                }

                if (blendShapeSync.TryGetComponent<SkinnedMeshRenderer>(out var targetRenderer))
                {
                    targetDropdownField.choices = GetBlendShapeNames(targetRenderer);
                    targetDropdownField.style.display = DisplayStyle.Flex;
                }
                else
                {
                    targetTextField.style.display = DisplayStyle.Flex;
                }

                sourceDropdownField.SetValueWithoutNotify(sourceProperty.stringValue);
                targetDropdownField.SetValueWithoutNotify(targetProperty.stringValue);
                sourceTextField.SetValueWithoutNotify(sourceProperty.stringValue);
                targetTextField.SetValueWithoutNotify(targetProperty.stringValue);

                sourceDropdownField.RegisterValueChangedCallback(e =>
                {
                    sourceProperty.stringValue = e.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                sourceTextField.RegisterValueChangedCallback(e =>
                {
                    sourceProperty.stringValue = e.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                targetDropdownField.RegisterValueChangedCallback(e =>
                {
                    targetProperty.stringValue = e.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                targetTextField.RegisterValueChangedCallback(e =>
                {
                    targetProperty.stringValue = e.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                weightFloatField.value = Mathf.Clamp(weightProperty.floatValue, 0f, 1f);
                weightFloatField.RegisterValueChangedCallback(e =>
                {
                    var value = Mathf.Clamp(e.newValue, 0f, 1f);
                    weightProperty.floatValue = value;
                    weightFloatField.value = value;
                    serializedObject.ApplyModifiedProperties();
                });
            };

            _syncedBlendShapesListView.itemsSource = blendShapeSync._blendShapes;
            _syncedBlendShapesListView.itemsRemoved += indexes =>
            {
                var blendShapesProperty = serializedObject.FindProperty(nameof(BlendShapeSync._blendShapes));
                var sortedIndexes = indexes.ToArray();
                Array.Sort(sortedIndexes);
                for (var i = sortedIndexes.Length - 1; i >= 0; i--)
                    blendShapesProperty.DeleteArrayElementAtIndex(sortedIndexes[i]);
                serializedObject.ApplyModifiedProperties();
            };

            L(
                en: "Source Renderer",
                ko: "소스 렌더러",
                setter: s => { _sourceRendererObjectField.label = s; }
            );

            L(
                en: "Source Renderer Path",
                ko: "소스 렌더러 경로",
                setter: s =>
                {
                    labelChanged = true;
                    _sourceRendererPathTextField.label = s;
                }
            );

            L(
                en: "Synced Blend Shapes",
                ko: "동기화된 블렌드 쉐이프",
                setter: s => { _syncedBlendShapesListView.headerTitle = s; }
            );

            L(
                en: "Source",
                ko: "원본",
                setter: s => { sourceLabel.text = s; }
            );

            L(
                en: "Target",
                ko: "대상",
                setter: s => { targetLabel.text = s; }
            );

            L(
                en: "Weight",
                ko: "가중치",
                setter: s => { weightLabel.text = s; }
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