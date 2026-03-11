using Bricscad.ApplicationServices;
using Teigha.Runtime;
using BricsLayerPlugin.Managers;

[assembly: ExtensionApplication(typeof(BricsLayerPlugin.PluginApp))]
[assembly: CommandClass(typeof(BricsLayerPlugin.Commands.ClassCommands))]
[assembly: CommandClass(typeof(BricsLayerPlugin.Commands.LevelCommands))]

namespace BricsLayerPlugin
{
    /// <summary>
    /// Vstupní bod pluginu pro BricsCAD.
    /// Implementuje organizaci výkresu ve stylu Vectorworks:
    /// - Třídy (Classes) = BricsCAD vrstvy (vizuální vlastnosti)
    /// - Hladiny (Levels) = virtuální organizace (draw order, seskupení)
    /// </summary>
    public class PluginApp : IExtensionApplication
    {
        public void Initialize()
        {
            var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n=== BricsLayerPlugin (Vectorworks styl) v2.0 ===");
            ed?.WriteMessage("\nTřídy (= BricsCAD vrstvy): VW_CLASSES, VW_CLASS_NEW, VW_CLASS_ACTIVE, VW_CLASS_LIST");
            ed?.WriteMessage("\nHladiny (organizace):       VW_LEVELS, VW_LEVEL_NEW, VW_LEVEL_ASSIGN, VW_LEVEL_STATE");
            ed?.WriteMessage("\n                            VW_LEVEL_UP, VW_LEVEL_DOWN, VW_LEVEL_SYNC, VW_LEVEL_LIST");
            ed?.WriteMessage("\nPanel:                      VW_PANEL (otevřít/zavřít)");
            ed?.WriteMessage("\nTřída = vzhled objektů (barva, čára, tloušťka, průhlednost)");
            ed?.WriteMessage("\nHladina = organizace objektů (pořadí zobrazení, viditelnost skupiny)");

            Application.DocumentManager.DocumentActivated += OnDocumentActivated;
        }

        public void Terminate()
        {
            Application.DocumentManager.DocumentActivated -= OnDocumentActivated;
        }

        private void OnDocumentActivated(object? sender, DocumentCollectionEventArgs e)
        {
            if (e.Document == null) return;

            ClassManager.Instance.LoadFromDocument(e.Document);
            LevelManager.Instance.LoadFromDocument(e.Document);
        }
    }
}
