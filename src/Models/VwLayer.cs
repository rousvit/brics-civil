using System;

namespace BricsLayerPlugin.Models
{
    /// <summary>
    /// Representace vrstvy ve stylu Vectorworks.
    /// Vrstva má pořadí (Order) které určuje zobrazení nad/pod ostatními vrstvami.
    /// </summary>
    public class VwLayer
    {
        public string Name { get; set; } = string.Empty;
        public VwLayerState State { get; set; } = VwLayerState.On;

        /// <summary>
        /// Pořadí vrstvy – nižší číslo = pod, vyšší = nahoře.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Název odpovídající BricsCAD vrstvy (LayerTableRecord).
        /// </summary>
        public string BricsLayerName { get; set; } = string.Empty;

        /// <summary>
        /// Uživatelský popis vrstvy.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        public override string ToString() => $"{Order}: {Name} [{State}]";
    }
}
