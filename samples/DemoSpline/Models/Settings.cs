using System;
using System.IO;
using System.Text.Json;

namespace DemoSpline.Models
{
    public sealed class Settings
    {
        public bool ShowGrid { get; set; } = true;

        public static string GetSettingsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DemoSpline");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }

        public static Settings Load()
        {
            try
            {
                var path = GetSettingsPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var s = JsonSerializer.Deserialize<Settings>(json);
                    if (s != null) return s;
                }
            }
            catch { }
            return new Settings();
        }

        public void Save()
        {
            try
            {
                var path = GetSettingsPath();
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }
}


