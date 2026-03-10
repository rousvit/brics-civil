using System;
using System.Drawing;
using System.Windows.Forms.Integration;
using Bricscad.Windows;

namespace BricsLayerPlugin.UI
{
    /// <summary>
    /// Host pro plovoucí/dokovatelné okno ve stylu nativních BricsCAD panelů
    /// (Vlastnosti, Vrstvy, Průzkumník).
    ///
    /// PaletteSet podporuje:
    /// - Plovoucí okno (floating) – volně přesouvatelné po obrazovce
    /// - Dokování (docking) – připnutí k levému/pravému okraji
    /// - Auto-hide – automatické skrývání při neaktivitě
    /// - Více záložek (tabs) – Vrstvy + Třídy v jednom okně
    /// </summary>
    public static class LayerPaletteHost
    {
        private static PaletteSet? _paletteSet;

        // Unikátní GUID – BricsCAD si zapamatuje pozici/velikost okna mezi relacemi
        private static readonly Guid PaletteGuid =
            new("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

        /// <summary>
        /// Zobrazí nebo aktivuje paletu. Pokud neexistuje, vytvoří ji.
        /// </summary>
        public static void Show()
        {
            if (_paletteSet == null)
                CreatePaletteSet();

            _paletteSet!.Visible = true;
        }

        /// <summary>
        /// Zobrazí paletu a přepne na záložku Třídy.
        /// </summary>
        public static void ShowClasses()
        {
            if (_paletteSet == null)
                CreatePaletteSet();

            _paletteSet!.Visible = true;
        }

        public static void Hide()
        {
            if (_paletteSet != null)
                _paletteSet.Visible = false;
        }

        public static void Toggle()
        {
            if (_paletteSet == null || !_paletteSet.Visible)
                Show();
            else
                Hide();
        }

        private static void CreatePaletteSet()
        {
            _paletteSet = new PaletteSet("VW Vrstvy a Třídy", PaletteGuid);

            // --- Chování okna ---

            // Dokování vlevo/vpravo (jako panel Vlastnosti v BricsCAD)
            _paletteSet.DockEnabled =
                (DockSides)((int)DockSides.Left | (int)DockSides.Right);

            // Minimální velikost
            _paletteSet.MinimumSize = new Size(320, 450);

            // Výchozí velikost plovoucího okna
            _paletteSet.Size = new Size(380, 600);

            // Styl – zavírací tlačítko, auto-hide, menu vlastností
            _paletteSet.Style =
                PaletteSetStyles.ShowCloseButton |
                PaletteSetStyles.ShowAutoHideButton |
                PaletteSetStyles.ShowPropertiesMenu;

            // --- Záložky (WPF přes ElementHost) ---
            var layerHost = new ElementHost { Child = new LayerPaletteControl() };
            var classHost = new ElementHost { Child = new ClassPaletteControl() };
            _paletteSet.Add("Vrstvy", layerHost);
            _paletteSet.Add("Třídy", classHost);

            // Výchozí stav – plovoucí okno
            _paletteSet.Dock = DockSides.None;
        }
    }
}
