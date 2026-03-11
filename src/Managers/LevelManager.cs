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

        // Auto-assign tracking
        private Document? _trackedDoc;
        private Database? _trackedDb;
        private readonly List<ObjectId> _pendingNewEntities = new();
        private bool _autoAssignEnabled;

        /// <summary>Vrátí aktivní hladinu.</summary>
        public VwLevel? ActiveLevel => _levels.FirstOrDefault(l => l.IsActive);

        /// <summary>
        /// Zapne automatické přiřazování nových entit do aktivní hladiny.
        /// Sleduje Database.ObjectAppended a přiřadí XData po dokončení příkazu.
        /// </summary>
        public void EnableAutoAssign(Document doc)
        {
            DisableAutoAssign();

            _trackedDoc = doc;
            _trackedDb = doc.Database;
            _autoAssignEnabled = true;

            _trackedDb.ObjectAppended += OnObjectAppended;
            _trackedDoc.CommandEnded += OnCommandEnded;
        }

        /// <summary>
        /// Vypne automatické přiřazování.
        /// </summary>
        public void DisableAutoAssign()
        {
            if (_trackedDb != null)
            {
                _trackedDb.ObjectAppended -= OnObjectAppended;
                _trackedDb = null;
            }
            if (_trackedDoc != null)
            {
                _trackedDoc.CommandEnded -= OnCommandEnded;
                _trackedDoc = null;
            }
            _pendingNewEntities.Clear();
            _autoAssignEnabled = false;
        }

        /// <summary>
        /// Při přidání nového objektu do databáze si zapamatujeme jeho ID.
        /// </summary>
        private void OnObjectAppended(object sender, ObjectEventArgs e)
        {
            if (!_autoAssignEnabled) return;
            if (ActiveLevel == null) return;

            // Zapamatovat ID nového objektu (pokud je to entita)
            if (e.DBObject is Entity)
                _pendingNewEntities.Add(e.DBObject.ObjectId);
        }

        /// <summary>
        /// Po dokončení příkazu přiřadíme všechny nové entity do aktivní hladiny.
        /// </summary>
        private void OnCommandEnded(object sender, CommandEventArgs e)
        {
            if (_pendingNewEntities.Count == 0) return;

            var activeLevel = ActiveLevel;
            var doc = _trackedDoc;
            var db = _trackedDb;

            // Zkopírovat a vyčistit pending list
            var pending = new List<ObjectId>(_pendingNewEntities);
            _pendingNewEntities.Clear();

            if (activeLevel == null || doc == null || db == null) return;

            // Ignorovat určité interní příkazy, které vytváří dočasné objekty
            var cmdName = e.GlobalCommandName?.ToUpperInvariant() ?? "";
            if (cmdName == "UNDO" || cmdName == "REDO" || cmdName == "U" ||
                cmdName == "REGEN" || cmdName == "REGENALL" || cmdName == "ZOOM" ||
                cmdName == "PAN" || cmdName == "REDRAW" || cmdName.StartsWith("VW_"))
                return;

            try
            {
                using var tr = db.TransactionManager.StartTransaction();
                EnsureRegApp(db, tr);

                var modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);

                foreach (var id in pending)
                {
                    if (id.IsNull || id.IsErased) continue;

                    try
                    {
                        var obj = tr.GetObject(id, OpenMode.ForRead);
                        if (obj is not Entity ent) continue;
                        if (ent.IsErased) continue;

                        // Jen entity v ModelSpace
                        if (ent.OwnerId != modelSpaceId) continue;

                        // Přeskočit pokud už má level přiřazený
                        var existing = ent.GetXDataForApplication(XDataAppName);
                        if (existing != null) continue;

                        // Přiřadit do aktivní hladiny
                        ent.UpgradeOpen();
                        ent.XData = new ResultBuffer(
                            new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName),
                            new TypedValue((int)DxfCode.ExtendedDataAsciiString, activeLevel.Name));
                    }
                    catch
                    {
                        // Přeskočit problematické entity (temp objects, erased, etc.)
                    }
                }

                tr.Commit();
            }
            catch
            {
                // Tiše ignorovat chyby při auto-assign
            }
        }

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

            // Zapnout auto-assign pro tento dokument
            EnableAutoAssign(doc);

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
                        ent.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
                        break;
                    case VwLevelState.Off:
                        ent.Visible = false;
                        break;
                    case VwLevelState.Grayed:
                        ent.Visible = true;
                        ent.Color = Color.FromColorIndex(ColorMethod.ByAci, 8);
                        break;
                }
            }

            doc.TransactionManager.QueueForGraphicsFlush();
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

            doc.TransactionManager.QueueForGraphicsFlush();
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
                    ent.XData = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName),
                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, targetLevelName)
                    );
                }
                else
                {
                    ent.XData = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName)
                    );
                }

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
