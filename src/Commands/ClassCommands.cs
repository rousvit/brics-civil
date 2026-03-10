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
    /// BricsCAD příkazy pro správu tříd (classes) ve stylu Vectorworks.
    /// </summary>
    public class ClassCommands
    {
        /// <summary>
        /// Otevře paletu pro správu tříd (záložka Třídy).
        /// </summary>
        [CommandMethod("VW_CLASSES")]
        public void OpenClassPalette()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            ClassManager.Instance.LoadFromDocument(doc);
            LayerPaletteHost.ShowClasses();
        }

        /// <summary>
        /// Vytvoří novou třídu.
        /// </summary>
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
                var cls = ClassManager.Instance.CreateClass(nameResult.StringResult);

                // Barva
                var colorOpts = new PromptIntegerOptions("\nZadejte index barvy (1-255): ");
                colorOpts.DefaultValue = 7;
                colorOpts.UseDefaultValue = true;
                var colorResult = ed.GetInteger(colorOpts);
                if (colorResult.Status == PromptStatus.OK)
                    cls.ColorIndex = colorResult.Value;

                // Typ čáry
                var ltOpts = new PromptStringOptions("\nZadejte typ čáry [Continuous]: ");
                ltOpts.AllowSpaces = false;
                var ltResult = ed.GetString(ltOpts);
                if (ltResult.Status == PromptStatus.OK && !string.IsNullOrEmpty(ltResult.StringResult))
                    cls.LinetypeName = ltResult.StringResult;

                // Tloušťka
                var lwOpts = new PromptDoubleOptions("\nZadejte tloušťku čáry v mm [0.25]: ");
                lwOpts.DefaultValue = 0.25;
                lwOpts.UseDefaultValue = true;
                var lwResult = ed.GetDouble(lwOpts);
                if (lwResult.Status == PromptStatus.OK)
                    cls.LineweightMm = lwResult.Value;

                ed.WriteMessage($"\nTřída '{cls.Name}' vytvořena: {cls}");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nChyba: {ex.Message}");
            }
        }

        /// <summary>
        /// Přiřadí třídu vybraným objektům.
        /// </summary>
        [CommandMethod("VW_CLASS_ASSIGN")]
        public void AssignClass()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString(new PromptStringOptions("\nZadejte název třídy: "));
            if (nameResult.Status != PromptStatus.OK) return;

            var cls = ClassManager.Instance.FindByName(nameResult.StringResult);
            if (cls == null)
            {
                ed.WriteMessage($"\nTřída '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            var selResult = ed.GetSelection();
            if (selResult.Status != PromptStatus.OK) return;

            int count = 0;
            foreach (SelectedObject selObj in selResult.Value)
            {
                ClassManager.Instance.AssignClassToEntity(selObj.ObjectId, cls, doc);
                count++;
            }

            ed.WriteMessage($"\nTřída '{cls.Name}' přiřazena {count} objektům.");
        }

        /// <summary>
        /// Aktualizuje vlastnosti třídy a aplikuje na přiřazené objekty.
        /// </summary>
        [CommandMethod("VW_CLASS_UPDATE")]
        public void UpdateClass()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString(new PromptStringOptions("\nZadejte název třídy k úpravě: "));
            if (nameResult.Status != PromptStatus.OK) return;

            var cls = ClassManager.Instance.FindByName(nameResult.StringResult);
            if (cls == null)
            {
                ed.WriteMessage($"\nTřída '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            var colorOpts = new PromptIntegerOptions($"\nNový index barvy [{cls.ColorIndex}]: ");
            colorOpts.DefaultValue = cls.ColorIndex;
            colorOpts.UseDefaultValue = true;
            var colorResult = ed.GetInteger(colorOpts);
            if (colorResult.Status == PromptStatus.OK)
                cls.ColorIndex = colorResult.Value;

            var ltOpts = new PromptStringOptions($"\nNový typ čáry [{cls.LinetypeName}]: ");
            ltOpts.AllowSpaces = false;
            var ltResult = ed.GetString(ltOpts);
            if (ltResult.Status == PromptStatus.OK && !string.IsNullOrEmpty(ltResult.StringResult))
                cls.LinetypeName = ltResult.StringResult;

            var lwOpts = new PromptDoubleOptions($"\nNová tloušťka čáry [{cls.LineweightMm}]: ");
            lwOpts.DefaultValue = cls.LineweightMm;
            lwOpts.UseDefaultValue = true;
            var lwResult = ed.GetDouble(lwOpts);
            if (lwResult.Status == PromptStatus.OK)
                cls.LineweightMm = lwResult.Value;

            ClassManager.Instance.UpdateClass(cls, doc);
            ed.WriteMessage($"\nTřída '{cls.Name}' aktualizována a aplikována na objekty.");
        }

        /// <summary>
        /// Vypíše seznam tříd.
        /// </summary>
        [CommandMethod("VW_CLASS_LIST")]
        public void ListClasses()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            ClassManager.Instance.LoadFromDocument(doc);

            ed.WriteMessage("\n--- Třídy (Vectorworks styl) ---");
            foreach (var cls in ClassManager.Instance.Classes)
            {
                ed.WriteMessage($"\n  {cls}");
            }
            ed.WriteMessage($"\n--- Celkem: {ClassManager.Instance.Classes.Count} ---");
        }
    }
}
