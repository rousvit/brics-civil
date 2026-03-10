using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BricsLayerPlugin.Models;

namespace BricsLayerPlugin.Utils
{
    /// <summary>
    /// Ukládání a načítání konfigurace vrstev/tříd do JSON souboru
    /// vedle DWG souboru (*.vwlayers.json).
    /// </summary>
    public static class LayerSerializer
    {
        private record LayerConfig(List<VwLayer> Layers, List<VwClass> Classes);

        /// <summary>
        /// Uloží konfiguraci vrstev a tříd vedle DWG souboru.
        /// </summary>
        public static void Save(string dwgPath, IReadOnlyList<VwLayer> layers, IReadOnlyList<VwClass> classes)
        {
            var configPath = GetConfigPath(dwgPath);
            var config = new LayerConfig(new List<VwLayer>(layers), new List<VwClass>(classes));
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            File.WriteAllText(configPath, json);
        }

        /// <summary>
        /// Načte konfiguraci ze souboru.
        /// </summary>
        public static (List<VwLayer> Layers, List<VwClass> Classes)? Load(string dwgPath)
        {
            var configPath = GetConfigPath(dwgPath);
            if (!File.Exists(configPath)) return null;

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<LayerConfig>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (config == null) return null;
            return (config.Layers, config.Classes);
        }

        private static string GetConfigPath(string dwgPath) =>
            Path.ChangeExtension(dwgPath, ".vwlayers.json");
    }
}
