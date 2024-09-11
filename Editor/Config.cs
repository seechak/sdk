using SEECHAK.SDK.Editor.Core.API;
using UnityEditor;
using UnityEngine;

namespace SEECHAK.SDK.Editor
{
    [InitializeOnLoad]
    public class Config
    {
        private static Data _value;

        static Config()
        {
            _value = Value;
        }

        public static Data Value
        {
            get
            {
                if (_value != null) return _value;

                var configJson = Resources.Load<TextAsset>("config");
                if (configJson == null) return null;
                _value = Request.Deserialize<Data>(configJson.text);
                if (_value == null) return null;
                Client.BaseURL = _value.BaseURL;
                return _value;
            }
        }

        public class Data
        {
            public string BaseURL { get; set; }
            public string WebsiteURL { get; set; }
        }
    }
}