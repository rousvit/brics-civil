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
        private bool _isRefreshing;

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
                _isRefreshing = true;
                try
                {
                    LevelListView.ItemsSource = null;
                    LevelListView.ItemsSource = LevelManager.Instance.Levels;
                    UpdateActiveLevelDisplay();
                }
                finally
                {
                    _isRefreshing = false;
                }
            });
        }

        private void UpdateActiveLevelDisplay()
        {
            var active = LevelManager.Instance.ActiveLevel;
            ActiveLevelText.Text = active != null ? active.Name : "(žádná)";
        }

        /// <summary>
        /// Set the correct ComboBox selection when the ComboBox loads in the DataTemplate.
        /// This fixes the broken SelectedItem binding to enum.
        /// </summary>
        private void StateCombo_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is VwLevel level)
            {
                _isRefreshing = true;
                combo.SelectedIndex = (int)level.State; // On=0, Off=1, Grayed=2
                _isRefreshing = false;
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (LevelListView.SelectedItem is not VwLevel level) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                using (doc.LockDocument())
                {
                    LevelManager.Instance.MoveUp(level);
                    LevelManager.Instance.SyncDrawOrder(doc);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (LevelListView.SelectedItem is not VwLevel level) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                using (doc.LockDocument())
                {
                    LevelManager.Instance.MoveDown(level);
                    LevelManager.Instance.SyncDrawOrder(doc);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteLevel_Click(object sender, RoutedEventArgs e)
        {
            if (LevelListView.SelectedItem is not VwLevel level) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

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
                using (doc.LockDocument())
                {
                    LevelManager.Instance.DeleteLevel(level, doc, targetLevel);
                }
            }
            catch (System.Exception ex)
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

        /// <summary>
        /// Přiřadit vybrané objekty do zvolené hladiny.
        /// Spustí příkaz VW_LEVEL_ASSIGN s předvyplněným názvem hladiny.
        /// </summary>
        private void AssignToLevel_Click(object sender, RoutedEventArgs e)
        {
            if (LevelListView.SelectedItem is not VwLevel level) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Launch the assign command with the level name pre-filled
            doc.SendStringToExecute("VW_LEVEL_ASSIGN\n" + level.Name + "\n", true, false, false);
        }

        /// <summary>
        /// Zjistit hladinu vybraných objektů.
        /// </summary>
        private void InfoLevel_Click(object sender, RoutedEventArgs e)
        {
            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            doc.SendStringToExecute("VW_LEVEL_INFO\n", true, false, false);
        }

        private void StateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRefreshing) return;
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

            try
            {
                using (doc.LockDocument())
                {
                    LevelManager.Instance.SetState(level, state, doc);
                }
                // Force screen refresh
                BcadApplication.UpdateScreen();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SyncOrder_Click(object sender, RoutedEventArgs e)
        {
            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                using (doc.LockDocument())
                {
                    LevelManager.Instance.SyncDrawOrder(doc);
                }
                BcadApplication.UpdateScreen();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                using (doc.LockDocument())
                {
                    LevelManager.Instance.LoadFromDocument(doc);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
