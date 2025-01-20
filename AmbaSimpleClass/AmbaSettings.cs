using System.IO;
using System.Text.Json;

namespace AmbaSimpleClass
{
    public class AmbaSettings
    {
        public string StoragePath { get; set; } = "D:\\AMBAStorage";
        public int SyncIntervalMinutes { get; set; } = 15;
        public string FileFormat { get; set; } = "{TIME}_{ID}{EXT}";
        public bool DeleteAfterSync { get; set; } = true;

        public static AmbaSettings Load(string filePath = "appsettings.json")
        {
            if (!File.Exists(filePath))
            {
                var defaultSettings = new AmbaSettings();
                File.WriteAllText(filePath, JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true }));
                return defaultSettings;
            }

            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<AmbaSettings>(json) ?? new AmbaSettings();
        }
    }
}
