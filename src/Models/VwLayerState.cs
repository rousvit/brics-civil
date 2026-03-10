namespace BricsLayerPlugin.Models
{
    /// <summary>
    /// Stav vrstvy ve stylu Vectorworks.
    /// </summary>
    public enum VwLayerState
    {
        /// <summary>Vrstva je viditelná a editovatelná.</summary>
        On,

        /// <summary>Vrstva je neviditelná.</summary>
        Off,

        /// <summary>Vrstva je viditelná, ale zašedlá (nelze editovat).</summary>
        Grayed
    }
}
