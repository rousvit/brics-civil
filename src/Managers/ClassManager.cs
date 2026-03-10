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
    /// Správce tříd (classes) ve stylu Vectorworks.
    /// Třída definuje vizuální vlastnosti objektů: barvu, styl čáry, tloušťku.
    /// Implementováno pomocí BricsCAD linetypes + XData pro mapování.
    /// </summary>
    public class ClassManager
    {
        private readonly List<VwClass> _classes = new();
        private static ClassManager? _instance;

        public static ClassManager Instance => _instance ??= new ClassManager();

        public IReadOnlyList<VwClass> Classes => _classes.AsReadOnly();

        public event Action? ClassesChanged;

        private const string XDataAppName = "VWCLASS";

        /// <summary>
        /// Zajistí registraci XData aplikace v databázi.
        /// </summary>
        private void EnsureRegApp(Database db, Transaction tr)
        {
            var rat = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
            if (rat.Has(XDataAppName)) return;

            rat.UpgradeOpen();
            var rar = new RegAppTableRecord { Name = XDataAppName };
            rat.Add(rar);
            tr.AddNewlyCreatedDBObject(rar, true);
        }

        /// <summary>
        /// Vytvoří novou třídu.
        /// </summary>
        public VwClass CreateClass(string name)
        {
            if (_classes.Any(c => c.Name == name))
                throw new InvalidOperationException($"Třída '{name}' již existuje.");

            var cls = new VwClass { Name = name };
            _classes.Add(cls);
            ClassesChanged?.Invoke();
            return cls;
        }

        /// <summary>
        /// Aktualizuje vizuální vlastnosti třídy a aplikuje je na všechny přiřazené objekty.
        /// </summary>
        public void UpdateClass(VwClass cls, Document doc)
        {
            ApplyClassToEntities(cls, doc);
            ClassesChanged?.Invoke();
        }

        /// <summary>
        /// Přiřadí třídu k entitě (uloží jako XData).
        /// </summary>
        public void AssignClassToEntity(ObjectId entityId, VwClass cls, Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            EnsureRegApp(db, tr);

            var ent = (Entity)tr.GetObject(entityId, OpenMode.ForWrite);

            // Uložit název třídy do XData
            var xdata = new ResultBuffer(
                new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName),
                new TypedValue((int)DxfCode.ExtendedDataAsciiString, cls.Name)
            );
            ent.XData = xdata;

            // Aplikovat vizuální vlastnosti
            ApplyClassProperties(ent, cls, tr, db);

            tr.Commit();
        }

        /// <summary>
        /// Aplikuje vlastnosti třídy na konkrétní entitu.
        /// </summary>
        private void ApplyClassProperties(Entity ent, VwClass cls, Transaction tr, Database db)
        {
            ent.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)cls.ColorIndex);

            // Nastavit typ čáry
            var ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
            if (ltt.Has(cls.LinetypeName))
            {
                ent.LinetypeId = ltt[cls.LinetypeName];
            }

            // Nastavit tloušťku čáry
            ent.LineWeight = GetLineWeight(cls.LineweightMm);
        }

        /// <summary>
        /// Aplikuje vlastnosti třídy na všechny entity přiřazené k této třídě.
        /// </summary>
        private void ApplyClassToEntities(VwClass cls, Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var btr = (BlockTableRecord)tr.GetObject(
                SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);

            foreach (ObjectId entId in btr)
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                var className = GetEntityClassName(ent);

                if (className == cls.Name)
                {
                    ent.UpgradeOpen();
                    ApplyClassProperties(ent, cls, tr, db);
                }
            }

            tr.Commit();
        }

        /// <summary>
        /// Přečte název třídy z XData entity.
        /// </summary>
        public string? GetEntityClassName(Entity ent)
        {
            var xdata = ent.GetXDataForApplication(XDataAppName);
            if (xdata == null) return null;

            var values = xdata.AsArray();
            if (values.Length >= 2 && values[1].TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                return values[1].Value as string;

            return null;
        }

        /// <summary>
        /// Najde třídu podle názvu.
        /// </summary>
        public VwClass? FindByName(string name) =>
            _classes.FirstOrDefault(c => c.Name == name);

        /// <summary>
        /// Smaže třídu (odstraní XData z entit).
        /// </summary>
        public void DeleteClass(VwClass cls, Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var btr = (BlockTableRecord)tr.GetObject(
                SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);

            foreach (ObjectId entId in btr)
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                var className = GetEntityClassName(ent);

                if (className == cls.Name)
                {
                    ent.UpgradeOpen();
                    // Smazat XData – nastaví na ByLayer
                    ent.XData = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDataAppName)
                    );
                    ent.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
                    ent.LinetypeId = db.ByLayerLinetype;
                    ent.LineWeight = LineWeight.ByLayer;
                }
            }

            tr.Commit();

            _classes.Remove(cls);
            ClassesChanged?.Invoke();
        }

        /// <summary>
        /// Načte třídy z existujících XData v dokumentu.
        /// </summary>
        public void LoadFromDocument(Document doc)
        {
            _classes.Clear();
            var db = doc.Database;
            var classNames = new HashSet<string>();

            using var tr = db.TransactionManager.StartTransaction();
            var btr = (BlockTableRecord)tr.GetObject(
                SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);

            foreach (ObjectId entId in btr)
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                var className = GetEntityClassName(ent);
                if (className != null)
                    classNames.Add(className);
            }

            tr.Commit();

            foreach (var name in classNames.OrderBy(n => n))
            {
                _classes.Add(new VwClass { Name = name });
            }

            ClassesChanged?.Invoke();
        }

        private static LineWeight GetLineWeight(double mm) => mm switch
        {
            <= 0.0 => LineWeight.LineWeight000,
            <= 0.05 => LineWeight.LineWeight005,
            <= 0.09 => LineWeight.LineWeight009,
            <= 0.13 => LineWeight.LineWeight013,
            <= 0.15 => LineWeight.LineWeight015,
            <= 0.18 => LineWeight.LineWeight018,
            <= 0.20 => LineWeight.LineWeight020,
            <= 0.25 => LineWeight.LineWeight025,
            <= 0.30 => LineWeight.LineWeight030,
            <= 0.35 => LineWeight.LineWeight035,
            <= 0.40 => LineWeight.LineWeight040,
            <= 0.50 => LineWeight.LineWeight050,
            <= 0.53 => LineWeight.LineWeight053,
            <= 0.60 => LineWeight.LineWeight060,
            <= 0.70 => LineWeight.LineWeight070,
            <= 0.80 => LineWeight.LineWeight080,
            <= 0.90 => LineWeight.LineWeight090,
            <= 1.00 => LineWeight.LineWeight100,
            <= 1.06 => LineWeight.LineWeight106,
            <= 1.20 => LineWeight.LineWeight120,
            <= 1.40 => LineWeight.LineWeight140,
            <= 1.58 => LineWeight.LineWeight158,
            <= 2.00 => LineWeight.LineWeight200,
            _ => LineWeight.LineWeight211
        };
    }
}
