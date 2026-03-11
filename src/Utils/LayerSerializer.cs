using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BricsLayerPlugin.Models;

namespace BricsLayerPlugin.Utils
{
    /// <summary>
    /// Ukládání a načítání konfigurace tříd a hladin do JSON souboru
    /// vedle DWG souboru (*.vwconfig.json).
    /// </summary>
    public static class LayerSerializer
    {
        private record VwConfig(List<VwClass> Classes, List<VwLevel> Levels);

        public static void Save(string dwgPath, IReadOnlyList<VwClass> classes, IReadOnlyList<VwLevel> levels)
        {
            var configPath = GetConfigPath(dwgPath);
            var config = new VwConfig(new List<VwClass>(classes), new List<VwLevel>(levels));
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            File.WriteAllText(configPath, json);
        }

        public static (List<VwClass> Classes, List<VwLevel> Levels)? Load(string dwgPath)
        {
            var configPath = GetConfigPath(dwgPath);
            if (!File.Exists(configPath)) return null;

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<VwConfig>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (config == null) return null;
            return (config.Classes, config.Levels);
        }

        private static string GetConfigPath(string dwgPath) =>
            Path.ChangeExtension(dwgPath, ".vwconfig.json");
    }
}
