using SEECHAK.SDK.Core.Avatar;
using UnityEditor;
using UnityEngine.UIElements;

namespace SEECHAK.SDK.Editor.Avatar
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DoNotInclude))]
    public class DoNotIncludeEditor : SeechakInspector
    {
        public override void SetupInspector()
        {
            CloneTreeFromResource("DoNotIncludeEditor");
            var descriptionLabel = Inspector.Q<Label>("DescriptionLabel");
            L(
                "이 컴포넌트가 있는 GameObject는 아바타에 포함되지 않습니다.\n또한 모듈러 아바타는 이 GameObject를 처리하지 않습니다.",
                "GameObject with this component will not be included in the avatar.\nAlso, Modular Avatar will not process this GameObject.",
                s => { descriptionLabel.text = s; }
            );
        }
    }
}