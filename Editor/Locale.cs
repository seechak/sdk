using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace SEECHAK.SDK.Editor
{
    public enum Language
    {
        EN,
        KO,
        UNLOADED
    }

    public class Locale
    {
        private Language language = Language.UNLOADED;

        public Locale()
        {
            LanguageChanged += language => { this.language = language; };
        }

        private static Action<Language> LanguageChanged { get; set; }

        private Language Language
        {
            get
            {
                if (language == Language.UNLOADED)
                {
                    var value = EditorPrefs.GetString("com.seechak.sdk.Editor.SeechakEditor.Language", "EN");
                    language = value switch
                    {
                        "EN" => Language.EN,
                        "KO" => Language.KO,
                        _ => Language.EN
                    };
                }

                return language;
            }
            set
            {
                language = value;
                EditorPrefs.SetString("com.seechak.sdk.Editor.SeechakEditor.Language", value.ToString());
                LanguageChanged?.Invoke(language);
            }
        }

        public void Enable()
        {
            language = Language;
        }

        public string GetLanguageName(Language lang)
        {
            return lang switch
            {
                Language.EN => "English",
                Language.KO => "한국어",
                _ => "Unknown"
            };
        }

        public void L(string ko, string en, Action<string> setter)
        {
            setter(LL(ko, en));
            LanguageChanged += language => { setter(LL(ko, en)); };
        }

        public string LL(string ko, string en)
        {
            return Language switch
            {
                Language.KO => ko,
                Language.EN => en,
                _ => en
            };
        }

        public void SetupLanguageDropdown(VisualElement element, string name)
        {
            var languageDropdown = element.Q<DropdownField>(name);
            var languages = Enum.GetValues(typeof(Language));

            var dropdownIndex = 0;
            for (var i = 0; i < languages.Length; i++)
            {
                var language = (Language) languages.GetValue(i);
                if (language == Language.UNLOADED) continue;

                if (language == Language)
                {
                    languageDropdown.index = dropdownIndex;
                    languageDropdown.value = GetLanguageName(language);
                }

                languageDropdown.choices.Add(GetLanguageName(language));
                dropdownIndex += 1;
            }

            languageDropdown.RegisterValueChangedCallback(e => { Language = (Language) languageDropdown.index; });

            LanguageChanged += language => { languageDropdown.value = GetLanguageName(language); };
        }
    }
}