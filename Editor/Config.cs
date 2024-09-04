using SEECHAK.SDK.Editor.Core.API;
using UnityEditor;
using UnityEngine;

namespace SEECHAK.SDK.Editor
{
    [InitializeOnLoad]
    public class Config
    {
        static Config()
        {
            var configJson = Resources.Load<TextAsset>("config");
            Value = Request.Deserialize<Data>(configJson.text);
            Client.BaseURL = Value.BaseURL;
        }

        public static Data Value { get; }

        public class Data
        {
            public string BaseURL { get; set; }
            public string WebsiteURL { get; set; }
        }
    }
}