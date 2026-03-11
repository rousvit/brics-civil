namespace BricsLayerPlugin.Models
{
    /// <summary>
    /// Třída (Class) ve stylu Vectorworks.
    /// Mapuje se přímo na BricsCAD Layer (LayerTableRecord).
    /// Definuje vizuální vlastnosti objektů: barvu, styl čáry, tloušťku, průhlednost.
    /// </summary>
    public class VwClass
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>ACI barva (1-255).</summary>
        public int ColorIndex { get; set; } = 7;

        /// <summary>Název typu čáry (Continuous, Dashed, …).</summary>
        public string LinetypeName { get; set; } = "Continuous";

        /// <summary>Tloušťka čáry v mm.</summary>
        public double LineweightMm { get; set; } = 0.25;

        /// <summary>Průhlednost 0-100 (0 = neprůhledné, 100 = plně průhledné).</summary>
        public int Transparency { get; set; } = 0;

        /// <summary>Viditelnost třídy (On/Off/Grayed).</summary>
        public VwClassVisibility Visibility { get; set; } = VwClassVisibility.On;

        /// <summary>Je tato třída aktivní (nové objekty se vytváří na ní)?</summary>
        public bool IsActive { get; set; }

        /// <summary>Název BricsCAD vrstvy (LayerTableRecord.Name).</summary>
        public string BricsLayerName { get; set; } = string.Empty;

        public override string ToString() => $"{Name} [Color={ColorIndex}, LT={LinetypeName}, LW={LineweightMm}mm]";
    }

    public enum VwClassVisibility
    {
        On,
        Off,
        Grayed
    }
}
