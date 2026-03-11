using System;
using System.Drawing;
using System.Windows.Forms.Integration;
using Bricscad.Windows;

namespace BricsLayerPlugin.UI
{
    /// <summary>
    /// Host pro plovoucí/dokovatelné okno ve stylu nativních BricsCAD panelů.
    /// Obsahuje dvě záložky: Třídy (Classes) a Hladiny (Levels).
    /// </summary>
    public static class LayerPaletteHost
    {
        private static PaletteSet? _paletteSet;

        private static readonly Guid PaletteGuid =
            new("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

        /// <summary>
        /// Zobrazí nebo aktivuje paletu (záložka Třídy).
        /// </summary>
        public static void Show()
        {
            if (_paletteSet == null)
                CreatePaletteSet();

            _paletteSet!.Visible = true;
        }

        /// <summary>
        /// Zobrazí paletu a přepne na záložku Hladiny.
        /// </summary>
        public static void ShowLevels()
        {
            if (_paletteSet == null)
                CreatePaletteSet();

            _paletteSet!.Visible = true;
            // Přepnout na druhou záložku (Hladiny)
            if (_paletteSet.Count > 1)
                _paletteSet.Activate(1);
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
            _paletteSet = new PaletteSet("VW Organizace", PaletteGuid);

            _paletteSet.DockEnabled =
                (DockSides)((int)DockSides.Left | (int)DockSides.Right);

            _paletteSet.MinimumSize = new Size(350, 450);
            _paletteSet.Size = new Size(420, 650);

            _paletteSet.Style =
                PaletteSetStyles.ShowCloseButton |
                PaletteSetStyles.ShowAutoHideButton |
                PaletteSetStyles.ShowPropertiesMenu;

            // Záložky: Třídy (= BricsCAD layers) a Hladiny (= virtuální organizace)
            var classHost = new ElementHost { Child = new ClassPaletteControl() };
            var levelHost = new ElementHost { Child = new LevelPaletteControl() };
            _paletteSet.Add("Třídy", classHost);
            _paletteSet.Add("Hladiny", levelHost);

            _paletteSet.Dock = DockSides.None;
        }
    }
}
