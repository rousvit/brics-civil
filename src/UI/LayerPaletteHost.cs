using System;
using Bricscad.Windows;

namespace BricsLayerPlugin.UI
{
    /// <summary>
    /// Host pro WPF paletu vrstev v BricsCAD paletovém okně.
    /// </summary>
    public static class LayerPaletteHost
    {
        private static PaletteSet? _paletteSet;

        public static void Show()
        {
            if (_paletteSet == null)
            {
                _paletteSet = new PaletteSet("Vrstvy VW",
                    new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"));
                _paletteSet.MinimumSize = new System.Drawing.Size(300, 400);
                _paletteSet.AddVisual("Vrstvy", new LayerPaletteControl());
            }

            _paletteSet.Visible = true;
        }

        public static void Hide()
        {
            if (_paletteSet != null)
                _paletteSet.Visible = false;
        }
    }
}
