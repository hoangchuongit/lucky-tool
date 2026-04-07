using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LuckBurn.Utils
{
    public class ComConfig
    {
        public string PortName { get; set; }
        public string STT { get; set; }
    }

    public class ComConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "com_settings.json");

        public static List<ComConfig> Load()
        {
            if (!File.Exists(ConfigPath))
                return new List<ComConfig>();

            return JsonSerializer.Deserialize<List<ComConfig>>(File.ReadAllText(ConfigPath)) ?? new List<ComConfig>();
        }

        public static void Save(List<ComConfig> list)
        {
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static string GetOrAssignSTT(string PortName, bool isDefault = false)
        {
            var list = Load();

            var existing = list.FirstOrDefault(x => x.PortName == PortName);
            if (existing != null)
                return existing.STT;
            if(isDefault) return string.Empty;
            var stt = (list.Count + 1).ToString();
            list.Add(new ComConfig { PortName = PortName, STT = stt });
            Save(list);
            return stt;
        }
    }
}
