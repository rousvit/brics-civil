using Bricscad.ApplicationServices;
using Bricscad.Runtime;
using BricsLayerPlugin.Managers;

[assembly: ExtensionApplication(typeof(BricsLayerPlugin.PluginApp))]
[assembly: CommandClass(typeof(BricsLayerPlugin.Commands.LayerCommands))]
[assembly: CommandClass(typeof(BricsLayerPlugin.Commands.ClassCommands))]

namespace BricsLayerPlugin
{
    /// <summary>
    /// Vstupní bod pluginu pro BricsCAD.
    /// Inicializuje správce vrstev a tříd po načtení.
    /// </summary>
    public class PluginApp : IExtensionApplication
    {
        public void Initialize()
        {
            var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n=== BricsLayerPlugin (Vectorworks styl) načten ===");
            ed?.WriteMessage("\nPříkazy: VW_LAYERS, VW_LAYER_NEW, VW_LAYER_STATE, VW_LAYER_UP, VW_LAYER_DOWN");
            ed?.WriteMessage("\n              VW_LAYER_SYNC, VW_LAYER_LIST");
            ed?.WriteMessage("\n              VW_CLASS_NEW, VW_CLASS_ASSIGN, VW_CLASS_UPDATE, VW_CLASS_LIST");

            // Registrovat handler pro otevření dokumentu
            Application.DocumentManager.DocumentActivated += OnDocumentActivated;
        }

        public void Terminate()
        {
            Application.DocumentManager.DocumentActivated -= OnDocumentActivated;
        }

        private void OnDocumentActivated(object? sender, DocumentCollectionEventArgs e)
        {
            if (e.Document == null) return;

            // Automaticky načíst vrstvy a třídy při přepnutí dokumentu
            LayerManager.Instance.LoadFromDocument(e.Document);
            ClassManager.Instance.LoadFromDocument(e.Document);
        }
    }
}
