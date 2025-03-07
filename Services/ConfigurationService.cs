using System.Text.Json;

namespace ShareShot.Services
{
    public class ConfigurationService
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        private static ConfigurationService? instance;
        private readonly JsonDocument config;

        private ConfigurationService()
        {
            var json = File.ReadAllText(ConfigPath);
            config = JsonDocument.Parse(json);
        }

        public static ConfigurationService Instance
        {
            get
            {
                instance ??= new ConfigurationService();
                return instance;
            }
        }

        public string GetImgurClientId()
        {
            return config.RootElement.GetProperty("Imgur").GetProperty("ClientId").GetString() ?? 
                   throw new InvalidOperationException("Imgur Client ID not found in configuration");
        }
    }
} 