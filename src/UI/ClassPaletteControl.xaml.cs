using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Bricscad.ApplicationServices;
using BcadApplication = Bricscad.ApplicationServices.Application;
using BricsLayerPlugin.Managers;
using BricsLayerPlugin.Models;

namespace BricsLayerPlugin.UI
{
    public partial class ClassPaletteControl : UserControl
    {
        private bool _isRefreshing;

        public ClassPaletteControl()
        {
            InitializeComponent();
            ClassManager.Instance.ClassesChanged += RefreshList;
            RefreshList();
        }

        private void RefreshList()
        {
            Dispatcher.Invoke(() =>
            {
                _isRefreshing = true;
                try
                {
                    ClassListView.ItemsSource = null;
                    ClassListView.ItemsSource = ClassManager.Instance.Classes;
                    UpdateActiveClassDisplay();
                }
                finally
                {
                    _isRefreshing = false;
                }
            });
        }

        private void UpdateActiveClassDisplay()
        {
            var active = ClassManager.Instance.ActiveClass;
            ActiveClassText.Text = active != null ? active.Name : "(žádná)";
        }

        private void VisibilityCombo_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is VwClass cls)
            {
                _isRefreshing = true;
                combo.SelectedIndex = (int)cls.Visibility;
                _isRefreshing = false;
            }
        }

        /// <summary>
        /// ► tlačítko - přepínač aktivní třídy přímo v řádku.
        /// </summary>
        private void ActivateClass_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.DataContext is not VwClass cls) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                using (doc.LockDocument())
                {
                    ClassManager.Instance.SetActive(cls, doc);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddClass_Click(object sender, RoutedEventArgs e)
        {
            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            List<string> linetypes;
            using (doc.LockDocument())
            {
                linetypes = ClassManager.Instance.GetAvailableLinetypes(doc);
            }

            var dialog = new ClassEditDialog(
                name: "",
                colorIndex: 7,
                linetype: "Continuous",
                lineweight: 0.25,
                transparency: 0,
                availableLinetypes: linetypes,
                isNew: true);

            if (dialog.ShowDialog() != true) return;

            try
            {
                using (doc.LockDocument())
                {
                    ClassManager.Instance.CreateClass(
                        dialog.ClassName, doc,
                        dialog.ColorIndex,
                        dialog.LinetypeName,
                        dialog.LineweightMm,
                        dialog.Transparency,
                        dialog.IsTrueColor,
                        dialog.ColorRed,
                        dialog.ColorGreen,
                        dialog.ColorBlue);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteClass_Click(object sender, RoutedEventArgs e)
        {
            if (ClassListView.SelectedItem is not VwClass cls) return;

            if (cls.BricsLayerName == "0")
            {
                MessageBox.Show("Vrstvu '0' nelze smazat.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Opravdu smazat třídu '{cls.Name}'?\nObjekty budou přesunuty na vrstvu '0'.",
                "Potvrdit smazání", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                using (doc.LockDocument())
                {
                    ClassManager.Instance.DeleteClass(cls, doc);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditClass_Click(object sender, RoutedEventArgs e)
        {
            if (ClassListView.SelectedItem is not VwClass cls) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            List<string> linetypes;
            using (doc.LockDocument())
            {
                linetypes = ClassManager.Instance.GetAvailableLinetypes(doc);
            }

            var dialog = new ClassEditDialog(
                name: cls.Name,
                colorIndex: cls.ColorIndex,
                linetype: cls.LinetypeName,
                lineweight: cls.LineweightMm,
                transparency: cls.Transparency,
                availableLinetypes: linetypes,
                isNew: false,
                isTrueColor: cls.IsTrueColor,
                colorR: cls.ColorRed,
                colorG: cls.ColorGreen,
                colorB: cls.ColorBlue);

            if (dialog.ShowDialog() != true) return;

            try
            {
                cls.ColorIndex = dialog.ColorIndex;
                cls.IsTrueColor = dialog.IsTrueColor;
                cls.ColorRed = dialog.ColorRed;
                cls.ColorGreen = dialog.ColorGreen;
                cls.ColorBlue = dialog.ColorBlue;
                cls.LinetypeName = dialog.LinetypeName;
                cls.LineweightMm = dialog.LineweightMm;
                cls.Transparency = dialog.Transparency;

                using (doc.LockDocument())
                {
                    ClassManager.Instance.UpdateClassProperties(cls, doc);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClassListView_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EditClass_Click(sender, e);
        }

        private void VisibilityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRefreshing) return;
            if (sender is not ComboBox combo) return;
            if (combo.DataContext is not VwClass cls) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var visText = (combo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var visibility = visText switch
            {
                "On" => VwClassVisibility.On,
                "Off" => VwClassVisibility.Off,
                "Grayed" => VwClassVisibility.Grayed,
                _ => VwClassVisibility.On
            };

            try
            {
                using (doc.LockDocument())
                {
                    ClassManager.Instance.SetVisibility(cls, visibility, doc);
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
                    ClassManager.Instance.LoadFromDocument(doc);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClassListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClassListView.SelectedItem is VwClass cls)
            {
                DetailPanel.Visibility = Visibility.Visible;
                DetailName.Text = cls.IsActive ? $"{cls.Name} (aktivní)" : cls.Name;
                DetailColorText.Text = cls.ColorDisplay;
                DetailLinetype.Text = cls.LinetypeName;
                DetailLineweight.Text = $"{cls.LineweightMm} mm";
                DetailTransparency.Text = $"{cls.Transparency}%";
                DetailColorSwatch.Fill = new SolidColorBrush(ClassEditDialog.GetDisplayColor(cls));
            }
            else
            {
                DetailPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}
