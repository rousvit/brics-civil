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
    /// Správce vrstev ve stylu Vectorworks.
    /// Zajišťuje pořadí vrstev, stavy (On/Off/Grayed) a synchronizaci s BricsCAD.
    /// </summary>
    public class LayerManager
    {
        private readonly List<VwLayer> _layers = new();
        private static LayerManager? _instance;

        public static LayerManager Instance => _instance ??= new LayerManager();

        public IReadOnlyList<VwLayer> Layers => _layers.AsReadOnly();

        public event Action? LayersChanged;

        /// <summary>
        /// Načte existující BricsCAD vrstvy a vytvoří z nich VwLayer záznamy.
        /// </summary>
        public void LoadFromDocument(Document doc)
        {
            _layers.Clear();
            var db = doc.Database;

            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            int order = 0;
            foreach (var id in lt)
            {
                var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                var layer = new VwLayer
                {
                    Name = ltr.Name,
                    BricsLayerName = ltr.Name,
                    Order = order++,
                    State = ltr.IsOff ? VwLayerState.Off :
                            ltr.IsFrozen ? VwLayerState.Grayed :
                            VwLayerState.On
                };
                _layers.Add(layer);
            }
            tr.Commit();

            LayersChanged?.Invoke();
        }

        /// <summary>
        /// Vytvoří novou vrstvu.
        /// </summary>
        public VwLayer CreateLayer(string name, Document doc)
        {
            var db = doc.Database;

            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);

            if (lt.Has(name))
                throw new InvalidOperationException($"Vrstva '{name}' již existuje.");

            var ltr = new LayerTableRecord { Name = name };
            lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
            tr.Commit();

            var layer = new VwLayer
            {
                Name = name,
                BricsLayerName = name,
                Order = _layers.Count,
                State = VwLayerState.On
            };
            _layers.Add(layer);

            LayersChanged?.Invoke();
            return layer;
        }

        /// <summary>
        /// Posune vrstvu o jednu pozici nahoru (vyšší pořadí = nahoře).
        /// </summary>
        public void MoveUp(VwLayer layer)
        {
            var idx = _layers.IndexOf(layer);
            if (idx < 0 || idx >= _layers.Count - 1) return;

            // Prohodit s vrstvou nad
            var other = _layers[idx + 1];
            (layer.Order, other.Order) = (other.Order, layer.Order);
            _layers[idx] = other;
            _layers[idx + 1] = layer;

            LayersChanged?.Invoke();
        }

        /// <summary>
        /// Posune vrstvu o jednu pozici dolů (nižší pořadí = pod).
        /// </summary>
        public void MoveDown(VwLayer layer)
        {
            var idx = _layers.IndexOf(layer);
            if (idx <= 0) return;

            var other = _layers[idx - 1];
            (layer.Order, other.Order) = (other.Order, layer.Order);
            _layers[idx] = other;
            _layers[idx - 1] = layer;

            LayersChanged?.Invoke();
        }

        /// <summary>
        /// Nastaví stav vrstvy a synchronizuje s BricsCAD.
        /// </summary>
        public void SetState(VwLayer layer, VwLayerState state, Document doc)
        {
            layer.State = state;
            SyncLayerState(layer, doc);
            LayersChanged?.Invoke();
        }

        /// <summary>
        /// Synchronizuje stav jedné VwLayer do BricsCAD LayerTableRecord.
        /// </summary>
        private void SyncLayerState(VwLayer layer, Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (!lt.Has(layer.BricsLayerName)) { tr.Commit(); return; }

            var ltr = (LayerTableRecord)tr.GetObject(lt[layer.BricsLayerName], OpenMode.ForWrite);

            switch (layer.State)
            {
                case VwLayerState.On:
                    ltr.IsOff = false;
                    ltr.IsFrozen = false;
                    break;
                case VwLayerState.Off:
                    ltr.IsOff = true;
                    ltr.IsFrozen = false;
                    break;
                case VwLayerState.Grayed:
                    ltr.IsOff = false;
                    ltr.IsFrozen = false;
                    // Grayed – objekty se zobrazí tlumenou barvou
                    ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 8); // šedá
                    break;
            }

            tr.Commit();
        }

        /// <summary>
        /// Synchronizuje pořadí vrstev do BricsCAD draw-order tabulky.
        /// </summary>
        public void SyncDrawOrder(Document doc)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var btr = (BlockTableRecord)tr.GetObject(
                SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

            var dot = (DrawOrderTable)tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite);
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            // Seřadit vrstvy dle pořadí a přesunout objekty
            foreach (var vwLayer in _layers.OrderBy(l => l.Order))
            {
                if (!lt.Has(vwLayer.BricsLayerName)) continue;

                var ids = new ObjectIdCollection();
                foreach (ObjectId entId in btr)
                {
                    var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                    if (ent.Layer == vwLayer.BricsLayerName)
                        ids.Add(entId);
                }

                if (ids.Count > 0)
                    dot.MoveToTop(ids);
            }

            tr.Commit();
        }

        /// <summary>
        /// Smaže vrstvu (musí být prázdná nebo se objekty přesunou na jinou vrstvu).
        /// </summary>
        public void DeleteLayer(VwLayer layer, Document doc, string? targetLayerName = null)
        {
            var db = doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (!lt.Has(layer.BricsLayerName)) { tr.Commit(); return; }

            // Přesunout objekty na cílovou vrstvu pokud je zadána
            if (targetLayerName != null)
            {
                var btr = (BlockTableRecord)tr.GetObject(
                    SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);

                foreach (ObjectId entId in btr)
                {
                    var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                    if (ent.Layer == layer.BricsLayerName)
                    {
                        ent.UpgradeOpen();
                        ent.Layer = targetLayerName;
                    }
                }
            }

            var ltr = (LayerTableRecord)tr.GetObject(lt[layer.BricsLayerName], OpenMode.ForWrite);
            ltr.Erase();
            tr.Commit();

            _layers.Remove(layer);
            ReorderLayers();
            LayersChanged?.Invoke();
        }

        /// <summary>
        /// Přepočítá pořadí po změnách.
        /// </summary>
        private void ReorderLayers()
        {
            for (int i = 0; i < _layers.Count; i++)
                _layers[i].Order = i;
        }

        /// <summary>
        /// Najde vrstvu podle názvu.
        /// </summary>
        public VwLayer? FindByName(string name) =>
            _layers.FirstOrDefault(l => l.Name == name);
    }
}
