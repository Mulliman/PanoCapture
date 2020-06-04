using Newtonsoft.Json;
using System.IO;

namespace PanoCapture.Plugin
{
    public class SettingsRepository
    {
        private readonly string _filename;

        public SettingsRepository(string filename)
        {
            _filename = filename;
        }

        public Settings Get()
        {
            if(!File.Exists(_filename))
            {
                return Default;
            }

            var text = File.ReadAllText(_filename);
            var settings = JsonConvert.DeserializeObject<Settings>(text);

            return settings;
        }

        public void Save(Settings settings)
        {
            var text = JsonConvert.SerializeObject(settings, Formatting.Indented);

            File.WriteAllText(_filename, text);
        }

        public static Settings Default => new Settings
        {
            HuginBinPath = @"C:\Program Files\Hugin\bin"
        };
    }
}