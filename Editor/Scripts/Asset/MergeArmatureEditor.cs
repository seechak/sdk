using SEECHAK.SDK.Core.Asset;
using UnityEditor;
using UnityEngine.UIElements;

namespace SEECHAK.SDK.Editor.Asset
{
    [CustomEditor(typeof(MergeArmature))]
    public class MergeArmatureEditor : SeechakInspector
    {
        public override void SetupInspector()
        {
            CloneTreeFromResource("MergeArmatureEditor");
            var descriptionLabel = Inspector.Q<Label>("DescriptionLabel");
            L(
                ko: "이 컴포넌트를 에셋의 Armature에 추가하면, 시착 시 아바타의 Armature에 자동으로 합쳐집니다.",
                en: "When you add this component to the Armature, it will be automatically merged into the Armature of the avatar when user try on.",
                setter: (s) =>
                {
                    descriptionLabel.text = s;
                }
            );
        }
    }
}