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
    /// Správce tříd (Classes) ve stylu Vectorworks.
    /// Třída = BricsCAD Layer. Řídí vizuální vlastnosti: barvu, typ čáry, tloušťku, průhlednost.
    /// Aktivní třída = aktivní BricsCAD vrstva (db.Clayer).
    /// </summary>
    public class ClassManager
    {
        private readonly List<VwClass> _classes = new();
        private static ClassManager? _instance;

        public static ClassManager Instance => _instance ??= new ClassManager();

        public IReadOnlyList<VwClass> Classes => _classes.AsReadOnly();

        public event Action? ClassesChanged;

        /// <summary>Vrátí aktuálně aktivní třídu.</summary>
        public VwClass? ActiveClass => _classes.FirstOrDefault(c => c.IsActive);

        /// <summary>
        /// Načte existující BricsCAD vrstvy jako třídy.
        /// </summary>
        public void LoadFromDocument(Document doc)
        {
            _classes.Clear();
            var db = doc.Database;

            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            var activeLayerId = db.Clayer;

            foreach (var id in lt)
            {
                var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);

                var visibility = VwClassVisibility.On;
                if (ltr.IsOff)
                    visibility = VwClassVisibility.Off;
                else if (ltr.IsFrozen)
                    visibility = VwClassVisibility.Grayed;

                var cls = new VwClass
                {
                    Name = ltr.Name,
                    BricsLayerName = ltr.Name,
                    ColorIndex = ltr.Color.ColorIndex,
                    LinetypeName = GetLinetypeName(ltr.LinetypeObjectId, tr),
                    LineweightMm = LineWeightToMm(ltr.LineWeight),
                    Transparency = GetTransparencyPercent(ltr.Transparency),
                    Visibility = visibility,
                    IsActive = id == activeLayerId
                };
                _classes.Add(cls);
            }
            tr.Commit();

            ClassesChanged?.Invoke();
        }

        /// <summary>
        /// Vytvoří novou třídu (= nový BricsCAD layer).
        /// </summary>
        public VwClass CreateClass(string name, Document doc,
            int colorIndex = 7, string linetype = "Continuous",
            double lineweightMm = 0.25, int transparency = 0)
        {
            if (_classes.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Třída '{name}' již existuje.");

            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);

            if (lt.Has(name))
                throw new InvalidOperationException($"BricsCAD vrstva '{name}' již existuje.");

            var ltr = new LayerTableRecord { Name = name };
            ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)Math.Clamp(colorIndex, 1, 255));
            ltr.LineWeight = MmToLineWeight(lineweightMm);

            // Linetype
            var ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
            if (ltt.Has(linetype))
                ltr.LinetypeObjectId = ltt[linetype];

            // Transparency
            if (transparency > 0)
            {
                byte alpha = (byte)(255 * ((100 - Math.Clamp(transparency, 0, 90)) / 100.0));
                ltr.Transparency = new Teigha.Colors.Transparency(alpha);
            }

            lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
            tr.Commit();

            var cls = new VwClass
            {
                Name = name,
                BricsLayerName = name,
                ColorIndex = colorIndex,
                LinetypeName = linetype,
                LineweightMm = lineweightMm,
                Transparency = transparency,
                Visibility = VwClassVisibility.On
            };
            _classes.Add(cls);
            ClassesChanged?.Invoke();
            return cls;
        }

        /// <summary>
        /// Nastaví třídu jako aktivní (= nastaví BricsCAD Clayer).
        /// </summary>
        public void SetActive(VwClass cls, Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (!lt.Has(cls.BricsLayerName)) { tr.Commit(); return; }

            db.Clayer = lt[cls.BricsLayerName];
            tr.Commit();

            foreach (var c in _classes) c.IsActive = false;
            cls.IsActive = true;
            ClassesChanged?.Invoke();
        }

        /// <summary>
        /// Nastaví viditelnost třídy (On/Off/Grayed).
        /// </summary>
        public void SetVisibility(VwClass cls, VwClassVisibility visibility, Document doc)
        {
            cls.Visibility = visibility;
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (!lt.Has(cls.BricsLayerName)) { tr.Commit(); return; }

            var ltr = (LayerTableRecord)tr.GetObject(lt[cls.BricsLayerName], OpenMode.ForWrite);

            switch (visibility)
            {
                case VwClassVisibility.On:
                    ltr.IsOff = false;
                    ltr.IsFrozen = false;
                    break;
                case VwClassVisibility.Off:
                    ltr.IsOff = true;
                    ltr.IsFrozen = false;
                    break;
                case VwClassVisibility.Grayed:
                    ltr.IsOff = false;
                    ltr.IsFrozen = true;
                    break;
            }

            tr.Commit();
            ClassesChanged?.Invoke();
        }

        /// <summary>
        /// Aktualizuje vlastnosti třídy (= vlastnosti BricsCAD vrstvy).
        /// </summary>
        public void UpdateClassProperties(VwClass cls, Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (!lt.Has(cls.BricsLayerName)) { tr.Commit(); return; }

            var ltr = (LayerTableRecord)tr.GetObject(lt[cls.BricsLayerName], OpenMode.ForWrite);

            ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)Math.Clamp(cls.ColorIndex, 1, 255));
            ltr.LineWeight = MmToLineWeight(cls.LineweightMm);

            var ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
            if (ltt.Has(cls.LinetypeName))
                ltr.LinetypeObjectId = ltt[cls.LinetypeName];

            if (cls.Transparency > 0)
            {
                byte alpha = (byte)(255 * ((100 - Math.Clamp(cls.Transparency, 0, 90)) / 100.0));
                ltr.Transparency = new Teigha.Colors.Transparency(alpha);
            }
            else
            {
                ltr.Transparency = new Teigha.Colors.Transparency(255);
            }

            tr.Commit();
            ClassesChanged?.Invoke();
        }

        /// <summary>
        /// Smaže třídu (= smaže BricsCAD vrstvu, objekty přesune na vrstvu "0").
        /// </summary>
        public void DeleteClass(VwClass cls, Document doc)
        {
            if (cls.BricsLayerName == "0")
                throw new InvalidOperationException("Vrstvu '0' nelze smazat.");

            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (lt.Has(cls.BricsLayerName))
            {
                // Přesunout objekty na vrstvu "0"
                var btr = (BlockTableRecord)tr.GetObject(
                    SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);

                foreach (ObjectId entId in btr)
                {
                    var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                    if (ent.Layer == cls.BricsLayerName)
                    {
                        ent.UpgradeOpen();
                        ent.Layer = "0";
                    }
                }

                // Pokud je to aktivní vrstva, přepnout na "0"
                if (db.Clayer == lt[cls.BricsLayerName])
                    db.Clayer = lt["0"];

                var ltr = (LayerTableRecord)tr.GetObject(lt[cls.BricsLayerName], OpenMode.ForWrite);
                ltr.Erase();
            }

            tr.Commit();

            _classes.Remove(cls);
            ClassesChanged?.Invoke();
        }

        /// <summary>Najde třídu podle názvu.</summary>
        public VwClass? FindByName(string name) =>
            _classes.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Vrátí seznam dostupných typů čar v dokumentu.</summary>
        public List<string> GetAvailableLinetypes(Document doc)
        {
            var result = new List<string>();
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);

            foreach (var id in ltt)
            {
                var lt = (LinetypeTableRecord)tr.GetObject(id, OpenMode.ForRead);
                result.Add(lt.Name);
            }
            tr.Commit();
            return result;
        }

        #region Helpers

        private static string GetLinetypeName(ObjectId linetypeId, Transaction tr)
        {
            if (linetypeId.IsNull) return "Continuous";
            var ltr = (LinetypeTableRecord)tr.GetObject(linetypeId, OpenMode.ForRead);
            return ltr.Name;
        }

        private static int GetTransparencyPercent(Teigha.Colors.Transparency trans)
        {
            if (!trans.IsByAlpha) return 0;
            int alpha = trans.Alpha;
            return (int)(100 - (alpha / 255.0 * 100));
        }

        private static double LineWeightToMm(LineWeight lw) => lw switch
        {
            LineWeight.LineWeight000 => 0.0,
            LineWeight.LineWeight005 => 0.05,
            LineWeight.LineWeight009 => 0.09,
            LineWeight.LineWeight013 => 0.13,
            LineWeight.LineWeight015 => 0.15,
            LineWeight.LineWeight018 => 0.18,
            LineWeight.LineWeight020 => 0.20,
            LineWeight.LineWeight025 => 0.25,
            LineWeight.LineWeight030 => 0.30,
            LineWeight.LineWeight035 => 0.35,
            LineWeight.LineWeight040 => 0.40,
            LineWeight.LineWeight050 => 0.50,
            LineWeight.LineWeight053 => 0.53,
            LineWeight.LineWeight060 => 0.60,
            LineWeight.LineWeight070 => 0.70,
            LineWeight.LineWeight080 => 0.80,
            LineWeight.LineWeight090 => 0.90,
            LineWeight.LineWeight100 => 1.00,
            LineWeight.LineWeight106 => 1.06,
            LineWeight.LineWeight120 => 1.20,
            LineWeight.LineWeight140 => 1.40,
            LineWeight.LineWeight158 => 1.58,
            LineWeight.LineWeight200 => 2.00,
            LineWeight.LineWeight211 => 2.11,
            _ => 0.25
        };

        internal static LineWeight MmToLineWeight(double mm) => mm switch
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

        #endregion
    }
}
