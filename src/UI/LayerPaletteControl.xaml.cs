using System;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using BricsLayerPlugin.Managers;
using BricsLayerPlugin.Models;

namespace BricsLayerPlugin.UI
{
    public partial class LayerPaletteControl : UserControl
    {
        public LayerPaletteControl()
        {
            InitializeComponent();
            LayerManager.Instance.LayersChanged += RefreshList;
            RefreshList();
        }

        private void RefreshList()
        {
            Dispatcher.Invoke(() =>
            {
                LayerListView.ItemsSource = null;
                LayerListView.ItemsSource = LayerManager.Instance.Layers;
            });
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (LayerListView.SelectedItem is VwLayer layer)
            {
                LayerManager.Instance.MoveUp(layer);
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                    LayerManager.Instance.SyncDrawOrder(doc);
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (LayerListView.SelectedItem is VwLayer layer)
            {
                LayerManager.Instance.MoveDown(layer);
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                    LayerManager.Instance.SyncDrawOrder(doc);
            }
        }

        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Jednoduchý input dialog
            var name = PromptForLayerName();
            if (string.IsNullOrEmpty(name)) return;

            try
            {
                LayerManager.Instance.CreateLayer(name, doc);
                LayerManager.Instance.SyncDrawOrder(doc);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteLayer_Click(object sender, RoutedEventArgs e)
        {
            if (LayerListView.SelectedItem is not VwLayer layer) return;

            var result = MessageBox.Show(
                $"Opravdu smazat vrstvu '{layer.Name}'?",
                "Potvrdit smazání", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                LayerManager.Instance.DeleteLayer(layer, doc, "0");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
                LayerManager.Instance.LoadFromDocument(doc);
        }

        private void SyncOrder_Click(object sender, RoutedEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
                LayerManager.Instance.SyncDrawOrder(doc);
        }

        private void LayerListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Budoucí rozšíření – zobrazení detailu vrstvy
        }

        private void StateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox combo) return;
            if (combo.DataContext is not VwLayer layer) return;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var stateText = (combo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var state = stateText switch
            {
                "On" => VwLayerState.On,
                "Off" => VwLayerState.Off,
                "Grayed" => VwLayerState.Grayed,
                _ => VwLayerState.On
            };

            LayerManager.Instance.SetState(layer, state, doc);
        }

        private string? PromptForLayerName()
        {
            var dialog = new InputDialog("Nová vrstva", "Zadejte název vrstvy:");
            return dialog.ShowDialog() == true ? dialog.InputText : null;
        }
    }
}
