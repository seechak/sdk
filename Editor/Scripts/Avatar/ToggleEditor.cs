using UnityEditor;
using UnityEngine.UIElements;

namespace SEECHAK.SDK.Editor.Avatar
{
    using SEECHAK.SDK.Core.Avatar;
    [CustomEditor(typeof(Toggle))]
    public class ToggleEditor : SeechakInspector
    {
        public override void SetupInspector()
        {
            CloneTreeFromResource("ToggleEditor");
            var descriptionLabel = Inspector.Q<Label>("DescriptionLabel");
            L(
                ko: "시착 시 옵션을 통해 이 컴포넌트가 있는 GameObject를 켜거나 끌 수 있습니다.",
                en: "You can turn on or off the GameObject with this component through the options when you try on.",
                setter: (s) =>
                {
                    descriptionLabel.text = s;
                }
            );
        }
    }
}