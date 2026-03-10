namespace BricsLayerPlugin.Models
{
    /// <summary>
    /// Třída (class) ve stylu Vectorworks.
    /// Definuje vizuální vlastnosti objektů – barvu, styl čáry, tloušťku.
    /// </summary>
    public class VwClass
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>ACI barva (1–255) nebo RGB jako int.</summary>
        public int ColorIndex { get; set; } = 7; // bílá/černá

        /// <summary>Název typu čáry (Continuous, Dashed, …).</summary>
        public string LinetypeName { get; set; } = "Continuous";

        /// <summary>Tloušťka čáry v mm.</summary>
        public double LineweightMm { get; set; } = 0.25;

        /// <summary>Je třída viditelná?</summary>
        public bool Visible { get; set; } = true;

        public override string ToString() => $"{Name} [Color={ColorIndex}, LT={LinetypeName}]";
    }
}
