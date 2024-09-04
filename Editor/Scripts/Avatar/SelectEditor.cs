using SEECHAK.SDK.Core.Avatar;
using UnityEditor;
using UnityEngine.UIElements;

namespace SEECHAK.SDK.Editor.Avatar
{
    [CustomEditor(typeof(Select))]
    public class SelectEditor : SeechakInspector
    {
        public override void SetupInspector()
        {
            CloneTreeFromResource("SelectEditor");
            var descriptionLabel = Inspector.Q<Label>("DescriptionLabel");
            L(
                ko: "시착 시 옵션을 통해 이 컴포넌트가 있는 GameObject의 자식 GameObject 중 하나만 켜지도록 설정할 수 있습니다.",
                en: "You can set one of the child GameObjects of the GameObject with this component to be turned on when you try on.",
                setter: (s) =>
                {
                    descriptionLabel.text = s;
                }
            );
        }
    }
}