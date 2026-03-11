using System;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using BcadApplication = Bricscad.ApplicationServices.Application;
using BricsLayerPlugin.Managers;
using BricsLayerPlugin.Models;
using Teigha.DatabaseServices;

namespace BricsLayerPlugin.UI
{
    public partial class LevelPaletteControl : UserControl
    {
        public LevelPaletteControl()
        {
            InitializeComponent();
            LevelManager.Instance.LevelsChanged += RefreshList;
            RefreshList();
        }

        private void RefreshList()
        {
            Dispatcher.Invoke(() =>
            {
                LevelListView.ItemsSource = null;
                LevelListView.ItemsSource = LevelManager.Instance.Levels;
                UpdateActiveLevelDisplay();
            });
        }

        private void UpdateActiveLevelDisplay()
        {
            var active = LevelManager.Instance.ActiveLevel;
            ActiveLevelText.Text = active != null ? active.Name : "(žádná)";
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (LevelListView.SelectedItem is VwLevel level)
            {
                LevelManager.Instance.MoveUp(level);
                var doc = BcadApplication.DocumentManager.MdiActiveDocument;
                if (doc != null)
                    LevelManager.Instance.SyncDrawOrder(doc);
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (LevelListView.SelectedItem is VwLevel level)
            {
                LevelManager.Instance.MoveDown(level);
                var doc = BcadApplication.DocumentManager.MdiActiveDocument;
                if (doc != null)
                    LevelManager.Instance.SyncDrawOrder(doc);
            }
        }

        private void AddLevel_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Nová hladina", "Zadejte název hladiny:");
            if (dialog.ShowDialog() != true) return;

            try
            {
                LevelManager.Instance.CreateLevel(dialog.InputText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteLevel_Click(object sender, RoutedEventArgs e)
        {
            if (LevelListView.SelectedItem is not VwLevel level) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Výběr cílové hladiny pro přesun objektů
            string? targetLevel = null;
            if (LevelManager.Instance.Levels.Count > 1)
            {
                var targetDialog = new InputDialog("Cílová hladina",
                    $"Kam přesunout objekty z hladiny '{level.Name}'?\n(Nechte prázdné pro odebrání z hladin)");
                if (targetDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(targetDialog.InputText))
                    targetLevel = targetDialog.InputText;
            }

            var result = MessageBox.Show(
                $"Opravdu smazat hladinu '{level.Name}'?",
                "Potvrdit smazání", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                LevelManager.Instance.DeleteLevel(level, doc, targetLevel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetActive_Click(object sender, RoutedEventArgs e)
        {
            if (LevelListView.SelectedItem is VwLevel level)
                LevelManager.Instance.SetActive(level);
        }

        private void LevelListView_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LevelListView.SelectedItem is VwLevel level)
                LevelManager.Instance.SetActive(level);
        }

        private void AssignToLevel_Click(object sender, RoutedEventArgs e)
        {
            if (LevelListView.SelectedItem is not VwLevel level) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var ed = doc.Editor;
            ed.WriteMessage($"\nVyberte objekty pro přiřazení do hladiny '{level.Name}'...");

            // Instrukce - výběr entit přes příkazovou řádku
            MessageBox.Show(
                $"Pro přiřazení objektů do hladiny '{level.Name}':\n\n" +
                $"1. Vyberte objekty v kreslicí ploše\n" +
                $"2. Zadejte příkaz: VW_LEVEL_ASSIGN\n" +
                $"3. Zadejte název hladiny: {level.Name}",
                "Přiřazení do hladiny", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox combo) return;
            if (combo.DataContext is not VwLevel level) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var stateText = (combo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var state = stateText switch
            {
                "On" => VwLevelState.On,
                "Off" => VwLevelState.Off,
                "Grayed" => VwLevelState.Grayed,
                _ => VwLevelState.On
            };

            LevelManager.Instance.SetState(level, state, doc);
        }

        private void SyncOrder_Click(object sender, RoutedEventArgs e)
        {
            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc != null)
                LevelManager.Instance.SyncDrawOrder(doc);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc != null)
                LevelManager.Instance.LoadFromDocument(doc);
        }
    }
}
