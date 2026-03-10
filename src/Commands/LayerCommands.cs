using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Bricscad.Runtime;
using Teigha.DatabaseServices;
using BricsLayerPlugin.Managers;
using BricsLayerPlugin.Models;
using BricsLayerPlugin.UI;

namespace BricsLayerPlugin.Commands
{
    /// <summary>
    /// BricsCAD příkazy pro správu vrstev ve stylu Vectorworks.
    /// </summary>
    public class LayerCommands
    {
        /// <summary>
        /// Otevře paletu pro správu vrstev.
        /// </summary>
        [CommandMethod("VW_LAYERS")]
        public void OpenLayerPalette()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            LayerManager.Instance.LoadFromDocument(doc);
            LayerPaletteHost.Show();
        }

        /// <summary>
        /// Vytvoří novou vrstvu.
        /// </summary>
        [CommandMethod("VW_LAYER_NEW")]
        public void CreateLayer()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString("\nZadejte název nové vrstvy: ");
            if (nameResult.Status != PromptStatus.OK) return;

            try
            {
                LayerManager.Instance.CreateLayer(nameResult.StringResult, doc);
                ed.WriteMessage($"\nVrstva '{nameResult.StringResult}' byla vytvořena.");
                LayerManager.Instance.SyncDrawOrder(doc);
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nChyba: {ex.Message}");
            }
        }

        /// <summary>
        /// Nastaví stav vrstvy (On/Off/Grayed).
        /// </summary>
        [CommandMethod("VW_LAYER_STATE")]
        public void SetLayerState()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString("\nZadejte název vrstvy: ");
            if (nameResult.Status != PromptStatus.OK) return;

            var layer = LayerManager.Instance.FindByName(nameResult.StringResult);
            if (layer == null)
            {
                ed.WriteMessage($"\nVrstva '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            var stateResult = ed.GetKeywords(
                new PromptKeywordOptions("\nZvolte stav [On/Off/Grayed]: ", "On Off Grayed"));
            if (stateResult.Status != PromptStatus.OK) return;

            var state = stateResult.StringResult switch
            {
                "On" => VwLayerState.On,
                "Off" => VwLayerState.Off,
                "Grayed" => VwLayerState.Grayed,
                _ => VwLayerState.On
            };

            LayerManager.Instance.SetState(layer, state, doc);
            ed.WriteMessage($"\nVrstva '{layer.Name}' nastavena na {state}.");
        }

        /// <summary>
        /// Posune vrstvu nahoru v pořadí.
        /// </summary>
        [CommandMethod("VW_LAYER_UP")]
        public void MoveLayerUp()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString("\nZadejte název vrstvy k posunutí nahoru: ");
            if (nameResult.Status != PromptStatus.OK) return;

            var layer = LayerManager.Instance.FindByName(nameResult.StringResult);
            if (layer == null)
            {
                ed.WriteMessage($"\nVrstva '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            LayerManager.Instance.MoveUp(layer);
            LayerManager.Instance.SyncDrawOrder(doc);
            ed.WriteMessage($"\nVrstva '{layer.Name}' posunuta nahoru (pořadí: {layer.Order}).");
        }

        /// <summary>
        /// Posune vrstvu dolů v pořadí.
        /// </summary>
        [CommandMethod("VW_LAYER_DOWN")]
        public void MoveLayerDown()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString("\nZadejte název vrstvy k posunutí dolů: ");
            if (nameResult.Status != PromptStatus.OK) return;

            var layer = LayerManager.Instance.FindByName(nameResult.StringResult);
            if (layer == null)
            {
                ed.WriteMessage($"\nVrstva '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            LayerManager.Instance.MoveDown(layer);
            LayerManager.Instance.SyncDrawOrder(doc);
            ed.WriteMessage($"\nVrstva '{layer.Name}' posunuta dolů (pořadí: {layer.Order}).");
        }

        /// <summary>
        /// Synchronizuje draw-order všech vrstev.
        /// </summary>
        [CommandMethod("VW_LAYER_SYNC")]
        public void SyncLayers()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            LayerManager.Instance.SyncDrawOrder(doc);
            doc.Editor.WriteMessage("\nPořadí vrstev synchronizováno.");
        }

        /// <summary>
        /// Vypíše seznam vrstev s jejich stavem a pořadím.
        /// </summary>
        [CommandMethod("VW_LAYER_LIST")]
        public void ListLayers()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            LayerManager.Instance.LoadFromDocument(doc);

            ed.WriteMessage("\n--- Vrstvy (Vectorworks styl) ---");
            foreach (var layer in LayerManager.Instance.Layers)
            {
                ed.WriteMessage($"\n  {layer}");
            }
            ed.WriteMessage($"\n--- Celkem: {LayerManager.Instance.Layers.Count} ---");
        }
    }
}
