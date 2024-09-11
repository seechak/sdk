namespace SEECHAK.SDK.Editor
{
    public class Config
    {
        private static Data _value;

        public static Data Value =>
            new()
            {
                BaseURL = "https://api.seechak.com",
                WebsiteURL = "https://seechak.com"
            };

        public class Data
        {
            public string BaseURL { get; set; }
            public string WebsiteURL { get; set; }
        }
    }
}