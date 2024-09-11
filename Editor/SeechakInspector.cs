using System;
using SEECHAK.SDK.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace SEECHAK.SDK.Editor
{
    public abstract class SeechakInspector : UnityEditor.Editor
    {
        private readonly Locale locale = new();
        protected VisualElement Inspector { get; private set; }

        public void OnEnable()
        {
            Inspector = new VisualElement();
            locale.Enable();
        }


        public abstract void SetupInspector();

        public override VisualElement CreateInspectorGUI()
        {
            var header = Resources.Load<VisualTreeAsset>("EditorHeader");
            var footer = Resources.Load<VisualTreeAsset>("EditorFooter");

            header.CloneTree(Inspector);
            SetupInspector();
            footer.CloneTree(Inspector);

            locale.SetupLanguageDropdown(Inspector, "LanguageDropdown");
            SetComponentName();
            return Inspector;
        }

        public void L(string ko, string en, Action<string> setter)
        {
            locale.L(ko, en, setter);
        }

        public string LL(string ko, string en)
        {
            return locale.LL(ko, en);
        }

        public void LLL(Action<Language> setter)
        {
            locale.LLL(setter);
        }

        private void SetComponentName()
        {
            var componentName = Inspector.Q<Label>("ComponentName");
            var component = (IComponent) target;
            if (component != null) componentName.text = component.Name;
        }

        protected void CloneTreeFromResource(string path)
        {
            var visualTree = Resources.Load<VisualTreeAsset>(path);
            visualTree.CloneTree(Inspector);
        }
    }
}