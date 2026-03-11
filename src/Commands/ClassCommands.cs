using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.Runtime;
using BricsLayerPlugin.Managers;
using BricsLayerPlugin.UI;

namespace BricsLayerPlugin.Commands
{
    /// <summary>
    /// BricsCAD příkazy pro správu tříd (= BricsCAD vrstev) ve stylu Vectorworks.
    /// </summary>
    public class ClassCommands
    {
        [CommandMethod("VW_CLASSES")]
        public void OpenClassPalette()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            ClassManager.Instance.LoadFromDocument(doc);
            LayerPaletteHost.Show();
        }

        [CommandMethod("VW_CLASS_NEW")]
        public void CreateClass()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString(new PromptStringOptions("\nZadejte název nové třídy: "));
            if (nameResult.Status != PromptStatus.OK) return;

            try
            {
                var colorOpts = new PromptIntegerOptions("\nIndex barvy ACI (1-255) [7]: ");
                colorOpts.DefaultValue = 7;
                colorOpts.UseDefaultValue = true;
                var colorResult = ed.GetInteger(colorOpts);
                int color = colorResult.Status == PromptStatus.OK ? colorResult.Value : 7;

                var ltOpts = new PromptStringOptions("\nTyp čáry [Continuous]: ");
                ltOpts.AllowSpaces = false;
                var ltResult = ed.GetString(ltOpts);
                string linetype = ltResult.Status == PromptStatus.OK && !string.IsNullOrEmpty(ltResult.StringResult)
                    ? ltResult.StringResult : "Continuous";

                var lwOpts = new PromptDoubleOptions("\nTloušťka čáry v mm [0.25]: ");
                lwOpts.DefaultValue = 0.25;
                lwOpts.UseDefaultValue = true;
                var lwResult = ed.GetDouble(lwOpts);
                double lw = lwResult.Status == PromptStatus.OK ? lwResult.Value : 0.25;

                var cls = ClassManager.Instance.CreateClass(nameResult.StringResult, doc, color, linetype, lw);
                ed.WriteMessage($"\nTřída '{cls.Name}' vytvořena: {cls}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nChyba: {ex.Message}");
            }
        }

        [CommandMethod("VW_CLASS_ACTIVE")]
        public void SetActiveClass()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString(new PromptStringOptions("\nZadejte název třídy k aktivaci: "));
            if (nameResult.Status != PromptStatus.OK) return;

            var cls = ClassManager.Instance.FindByName(nameResult.StringResult);
            if (cls == null)
            {
                ed.WriteMessage($"\nTřída '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            ClassManager.Instance.SetActive(cls, doc);
            ed.WriteMessage($"\nAktivní třída: '{cls.Name}'");
        }

        [CommandMethod("VW_CLASS_LIST")]
        public void ListClasses()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            ClassManager.Instance.LoadFromDocument(doc);

            ed.WriteMessage("\n--- Třídy (= BricsCAD vrstvy) ---");
            foreach (var cls in ClassManager.Instance.Classes)
            {
                var active = cls.IsActive ? " [AKTIVNÍ]" : "";
                ed.WriteMessage($"\n  {cls}{active}");
            }
            ed.WriteMessage($"\n--- Celkem: {ClassManager.Instance.Classes.Count} ---");
        }
    }
}
