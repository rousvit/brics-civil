using System;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Teigha.Runtime;
using Teigha.DatabaseServices;
using BricsLayerPlugin.Managers;
using BricsLayerPlugin.Models;
using BricsLayerPlugin.UI;

namespace BricsLayerPlugin.Commands
{
    /// <summary>
    /// BricsCAD příkazy pro správu hladin (Levels) ve stylu Vectorworks Design Layers.
    /// Hladiny = virtuální organizační systém nad entitami.
    /// </summary>
    public class LevelCommands
    {
        [CommandMethod("VW_LEVELS")]
        public void OpenLevelPalette()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            LevelManager.Instance.LoadFromDocument(doc);
            LayerPaletteHost.ShowLevels();
        }

        [CommandMethod("VW_LEVEL_NEW")]
        public void CreateLevel()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString(new PromptStringOptions("\nZadejte název nové hladiny: "));
            if (nameResult.Status != PromptStatus.OK) return;

            try
            {
                var level = LevelManager.Instance.CreateLevel(nameResult.StringResult);
                ed.WriteMessage($"\nHladina '{level.Name}' vytvořena.");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nChyba: {ex.Message}");
            }
        }

        [CommandMethod("VW_LEVEL_ASSIGN")]
        public void AssignToLevel()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString(new PromptStringOptions("\nZadejte název hladiny: "));
            if (nameResult.Status != PromptStatus.OK) return;

            var level = LevelManager.Instance.FindByName(nameResult.StringResult);
            if (level == null)
            {
                ed.WriteMessage($"\nHladina '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            var selResult = ed.GetSelection();
            if (selResult.Status != PromptStatus.OK) return;

            var ids = new ObjectIdCollection();
            foreach (SelectedObject selObj in selResult.Value)
                ids.Add(selObj.ObjectId);

            LevelManager.Instance.AssignEntitiesToLevel(ids, level, doc);
            ed.WriteMessage($"\nHladina '{level.Name}' přiřazena {ids.Count} objektům.");
        }

        [CommandMethod("VW_LEVEL_STATE")]
        public void SetLevelState()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString(new PromptStringOptions("\nZadejte název hladiny: "));
            if (nameResult.Status != PromptStatus.OK) return;

            var level = LevelManager.Instance.FindByName(nameResult.StringResult);
            if (level == null)
            {
                ed.WriteMessage($"\nHladina '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            var stateResult = ed.GetKeywords(
                new PromptKeywordOptions("\nZvolte stav [On/Off/Grayed]: ", "On Off Grayed"));
            if (stateResult.Status != PromptStatus.OK) return;

            var state = stateResult.StringResult switch
            {
                "On" => VwLevelState.On,
                "Off" => VwLevelState.Off,
                "Grayed" => VwLevelState.Grayed,
                _ => VwLevelState.On
            };

            LevelManager.Instance.SetState(level, state, doc);
            ed.WriteMessage($"\nHladina '{level.Name}' nastavena na {state}.");
        }

        [CommandMethod("VW_LEVEL_UP")]
        public void MoveLevelUp()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString(new PromptStringOptions("\nZadejte název hladiny k posunutí nahoru: "));
            if (nameResult.Status != PromptStatus.OK) return;

            var level = LevelManager.Instance.FindByName(nameResult.StringResult);
            if (level == null)
            {
                ed.WriteMessage($"\nHladina '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            LevelManager.Instance.MoveUp(level);
            LevelManager.Instance.SyncDrawOrder(doc);
            ed.WriteMessage($"\nHladina '{level.Name}' posunuta nahoru (pořadí: {level.Order}).");
        }

        [CommandMethod("VW_LEVEL_DOWN")]
        public void MoveLevelDown()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            var nameResult = ed.GetString(new PromptStringOptions("\nZadejte název hladiny k posunutí dolů: "));
            if (nameResult.Status != PromptStatus.OK) return;

            var level = LevelManager.Instance.FindByName(nameResult.StringResult);
            if (level == null)
            {
                ed.WriteMessage($"\nHladina '{nameResult.StringResult}' nebyla nalezena.");
                return;
            }

            LevelManager.Instance.MoveDown(level);
            LevelManager.Instance.SyncDrawOrder(doc);
            ed.WriteMessage($"\nHladina '{level.Name}' posunuta dolů (pořadí: {level.Order}).");
        }

        [CommandMethod("VW_LEVEL_SYNC")]
        public void SyncLevels()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            LevelManager.Instance.SyncDrawOrder(doc);
            doc.Editor.WriteMessage("\nPořadí hladin synchronizováno.");
        }

        [CommandMethod("VW_LEVEL_LIST")]
        public void ListLevels()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            LevelManager.Instance.LoadFromDocument(doc);

            ed.WriteMessage("\n--- Hladiny (Vectorworks Design Layers) ---");
            foreach (var level in LevelManager.Instance.Levels)
            {
                var active = level.IsActive ? " [AKTIVNÍ]" : "";
                ed.WriteMessage($"\n  {level}{active}");
            }
            ed.WriteMessage($"\n--- Celkem: {LevelManager.Instance.Levels.Count} ---");
        }

        [CommandMethod("VW_PANEL")]
        public void TogglePanel()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            ClassManager.Instance.LoadFromDocument(doc);
            LevelManager.Instance.LoadFromDocument(doc);
            LayerPaletteHost.Toggle();
        }
    }
}
