using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Bricscad.Runtime;
using Teigha.DatabaseServices;
using BricsLayerPlugin.Managers;
using BricsLayerPlugin.Models;

namespace BricsLayerPlugin.Commands
{
    /// <summary>
    /// BricsCAD příkazy pro správu tříd (classes) ve stylu Vectorworks.
    /// </summary>
    public class ClassCommands
    {
        /// <summary>
        /// Vytvoří novou třídu.
        /// </summary>
        [CommandMethod("VW_CLASS_NEW")]
        public void CreateClass()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString("\nZadejte název nové třídy: ");
            if (nameResult.Status != PromptStatus.OK) return;

            try
            {
                var cls = ClassManager.Instance.CreateClass(nameResult.StringResult);

                // Barva
                var colorResult = ed.GetInteger("\nZadejte index barvy (1-255): ");
                if (colorResult.Status == PromptStatus.OK)
                    cls.ColorIndex = colorResult.Value;

                // Typ čáry
                var ltResult = ed.GetString("\nZadejte typ čáry [Continuous]: ");
                if (ltResult.Status == PromptStatus.OK && !string.IsNullOrEmpty(ltResult.StringResult))
                    cls.LinetypeName = ltResult.StringResult;

                // Tloušťka
                var lwResult = ed.GetDouble("\nZadejte tloušťku čáry v mm [0.25]: ");
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

            var nameResult = ed.GetString("\nZadejte název třídy: ");
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

            var nameResult = ed.GetString("\nZadejte název třídy k úpravě: ");
            if (nameResult.Status != PromptStatus.OK) return;

            var cls = ClassManager.Instance.FindByName(nameResult.StringResult);
            if (cls == null)
            {
                ed.WriteMessage($"\nTřída '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            var colorResult = ed.GetInteger($"\nNový index barvy [{cls.ColorIndex}]: ");
            if (colorResult.Status == PromptStatus.OK)
                cls.ColorIndex = colorResult.Value;

            var ltResult = ed.GetString($"\nNový typ čáry [{cls.LinetypeName}]: ");
            if (ltResult.Status == PromptStatus.OK && !string.IsNullOrEmpty(ltResult.StringResult))
                cls.LinetypeName = ltResult.StringResult;

            var lwResult = ed.GetDouble($"\nNová tloušťka čáry [{cls.LineweightMm}]: ");
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
