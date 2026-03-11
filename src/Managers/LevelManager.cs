using System;
using System.Collections.Generic;
using System.Linq;
using BricsLayerPlugin.Models;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Colors;

namespace BricsLayerPlugin.Managers
{
    /// <summary>
    /// Správce hladin (Levels) ve stylu Vectorworks Design Layers.
    /// Hladiny jsou virtuální organizační systém nad BricsCAD entitami.
    /// Každá entita má XData tag s názvem hladiny.
    /// Hladiny řídí:
    /// - Pořadí zobrazení (draw order) – vyšší hladina = vykresleno nahoře
    /// - Hromadnou viditelnost – skrytí/zobrazení všech objektů v hladině
    /// - Organizační seskupení objektů
    /// </summary>
    public class LevelManager
    {
        private readonly List<VwLevel> _levels = new();
        private static LevelManager? _instance;

        public static LevelManager Instance => _instance ??= new LevelManager();

        public IReadOnlyList<VwLevel> Levels => _levels.AsReadOnly();

        public event Action? LevelsChanged;

        private const string XDataAppName = "VWLEVEL";

        /// <summary>Vrátí aktivní hladinu.</summary>
        public VwLevel? ActiveLevel => _levels.FirstOrDefault(l => l.IsActive);

        /// <summary>
        /// Načte hladiny z XData entit v dokumentu.
        /// Vytvoří výchozí hladinu "Výchozí" pokud žádná neexistuje.
        /// </summary>
        public void LoadFromDocument(Document doc)
        {
            _levels.Clear();
            var db = doc.Database;
            var levelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var tr = db.TransactionManager.StartTransaction();
            EnsureRegApp(db, tr);

            var btr = (BlockTableRecord)tr.GetObject(
                SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);

            foreach (ObjectId entId in btr)
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                var levelName = GetEntityLevelName(ent);
                if (levelName != null)
                    levelNames.Add(levelName);
            }

            tr.Commit();

            // Načíst z uloženého pořadí nebo vytvořit nové
            int order = 0;
            foreach (var name in levelNames.OrderBy(n => n))
            {
                _levels.Add(new VwLevel
                {
                    Name = name,
                    Order = order++,
                    State = VwLevelState.On
                });
            }

            // Výchozí hladina pokud žádná neexistuje
            if (_levels.Count == 0)
            {
                _levels.Add(new VwLevel
                {
                    Name = "Výchozí",
                    Order = 0,
                    State = VwLevelState.On,
                    IsActive = true
                });
            }
            else
            {
                _levels[0].IsActive = true;
            }

            LevelsChanged?.Invoke();
        }

        /// <summary>
        /// Vytvoří novou hladinu.
        /// </summary>
        public VwLevel CreateLevel(string name)
        {
            if (_levels.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Hladina '{name}' již existuje.");

            var level = new VwLevel
            {
                Name = name,
                Order = _levels.Count,
                State = VwLevelState.On
            };
            _levels.Add(level);
            LevelsChanged?.Invoke();
            return level;
        }

        /// <summary>
        /// Nastaví hladinu jako aktivní.
        /// </summary>
        public void SetActive(VwLevel level)
        {
            foreach (var l in _levels) l.IsActive = false;
            level.IsActive = true;
            LevelsChanged?.Invoke();
        }

        /// <summary>
        /// Přiřadí entitu do hladiny (uloží název hladiny do XData).
        /// </summary>
        public void AssignEntityToLevel(ObjectId entityId, VwLevel level, Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            EnsureRegApp(db, tr);

            var ent = (Entity)tr.GetObject(entityId, OpenMode.ForWrite);
            ent.XData = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, level.Name)
            );

            tr.Commit();
        }

        /// <summary>
        /// Přiřadí více entit do hladiny najednou.
        /// </summary>
        public void AssignEntitiesToLevel(ObjectIdCollection entityIds, VwLevel level, Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            EnsureRegApp(db, tr);

            foreach (ObjectId entId in entityIds)
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForWrite);
                ent.XData = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, level.Name)
                );
            }

            tr.Commit();
        }

        /// <summary>
        /// Nastaví stav hladiny a synchronizuje viditelnost entit.
        /// </summary>
        public void SetState(VwLevel level, VwLevelState state, Document doc)
        {
            level.State = state;
            SyncLevelVisibility(level, doc);
            LevelsChanged?.Invoke();
        }

        /// <summary>
        /// Synchronizuje viditelnost entit v dané hladině.
        /// On = viditelné, Off = neviditelné, Grayed = viditelné ale šedé.
        /// </summary>
        private void SyncLevelVisibility(VwLevel level, Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var btr = (BlockTableRecord)tr.GetObject(
                SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);

            foreach (ObjectId entId in btr)
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                var entLevel = GetEntityLevelName(ent);

                if (entLevel != level.Name) continue;

                ent.UpgradeOpen();

                switch (level.State)
                {
                    case VwLevelState.On:
                        ent.Visible = true;
                        // Obnovit barvu dle vrstvy (třídy)
                        ent.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
                        break;
                    case VwLevelState.Off:
                        ent.Visible = false;
                        break;
                    case VwLevelState.Grayed:
                        ent.Visible = true;
                        // Šedá barva pro vizuální odlišení
                        ent.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
                        break;
                }
            }

            tr.Commit();
        }

        /// <summary>
        /// Posune hladinu nahoru (vyšší pořadí = vykresleno nahoře).
        /// </summary>
        public void MoveUp(VwLevel level)
        {
            var idx = _levels.IndexOf(level);
            if (idx < 0 || idx >= _levels.Count - 1) return;

            var other = _levels[idx + 1];
            (level.Order, other.Order) = (other.Order, level.Order);
            _levels[idx] = other;
            _levels[idx + 1] = level;

            LevelsChanged?.Invoke();
        }

        /// <summary>
        /// Posune hladinu dolů.
        /// </summary>
        public void MoveDown(VwLevel level)
        {
            var idx = _levels.IndexOf(level);
            if (idx <= 0) return;

            var other = _levels[idx - 1];
            (level.Order, other.Order) = (other.Order, level.Order);
            _levels[idx] = other;
            _levels[idx - 1] = level;

            LevelsChanged?.Invoke();
        }

        /// <summary>
        /// Synchronizuje draw order všech entit dle pořadí hladin.
        /// Entity v hladině s vyšším Order se vykreslí nahoře.
        /// </summary>
        public void SyncDrawOrder(Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var btr = (BlockTableRecord)tr.GetObject(
                SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);
            var dot = (DrawOrderTable)tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite);

            // Seřadit hladiny od nejnižší po nejvyšší
            foreach (var level in _levels.OrderBy(l => l.Order))
            {
                var ids = new ObjectIdCollection();
                foreach (ObjectId entId in btr)
                {
                    var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                    var entLevel = GetEntityLevelName(ent);
                    if (entLevel == level.Name)
                        ids.Add(entId);
                }

                if (ids.Count > 0)
                    dot.MoveToTop(ids);
            }

            tr.Commit();
        }

        /// <summary>
        /// Smaže hladinu. Odstraní XData z entit (entity zůstanou nepřiřazené).
        /// </summary>
        public void DeleteLevel(VwLevel level, Document doc, string? targetLevelName = null)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            EnsureRegApp(db, tr);

            var btr = (BlockTableRecord)tr.GetObject(
                SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);

            foreach (ObjectId entId in btr)
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                var entLevel = GetEntityLevelName(ent);

                if (entLevel != level.Name) continue;

                ent.UpgradeOpen();

                if (targetLevelName != null)
                {
                    // Přesunout na jinou hladinu
                    ent.XData = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName),
                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, targetLevelName)
                    );
                }
                else
                {
                    // Odstranit XData
                    ent.XData = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName)
                    );
                }

                // Obnovit viditelnost
                ent.Visible = true;
                ent.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
            }

            tr.Commit();

            _levels.Remove(level);
            ReorderLevels();
            LevelsChanged?.Invoke();
        }

        /// <summary>Najde hladinu podle názvu.</summary>
        public VwLevel? FindByName(string name) =>
            _levels.FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Přečte název hladiny z XData entity.</summary>
        public static string? GetEntityLevelName(Entity ent)
        {
            var xdata = ent.GetXDataForApplication(XDataAppName);
            if (xdata == null) return null;

            var values = xdata.AsArray();
            if (values.Length >= 2 && values[1].TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                return values[1].Value as string;

            return null;
        }

        /// <summary>
        /// Synchronizuje viditelnost všech hladin.
        /// </summary>
        public void SyncAllVisibility(Document doc)
        {
            foreach (var level in _levels)
            {
                if (level.State != VwLevelState.On)
                    SyncLevelVisibility(level, doc);
            }
        }

        private void EnsureRegApp(Database db, Transaction tr)
        {
            var rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (rat.Has(XDataAppName)) return;

            rat.UpgradeOpen();
            var rar = new RegAppTableRecord { Name = XDataAppName };
            rat.Add(rar);
            tr.AddNewlyCreatedDBObject(rar, true);
        }

        private void ReorderLevels()
        {
            for (int i = 0; i < _levels.Count; i++)
                _levels[i].Order = i;
        }
    }
}
