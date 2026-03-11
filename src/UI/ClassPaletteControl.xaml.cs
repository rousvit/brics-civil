using System;
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
                ClassListView.ItemsSource = null;
                ClassListView.ItemsSource = ClassManager.Instance.Classes;
                UpdateActiveClassDisplay();
            });
        }

        private void UpdateActiveClassDisplay()
        {
            var active = ClassManager.Instance.ActiveClass;
            ActiveClassText.Text = active != null ? active.Name : "(žádná)";
        }

        private void AddClass_Click(object sender, RoutedEventArgs e)
        {
            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var nameDialog = new InputDialog("Nová třída", "Zadejte název třídy:");
            if (nameDialog.ShowDialog() != true) return;

            try
            {
                // Dialog pro barvu
                int colorIndex = 7;
                var colorDialog = new InputDialog("Barva", "Index barvy ACI (1-255) [7]:");
                if (colorDialog.ShowDialog() == true && int.TryParse(colorDialog.InputText, out var ci))
                    colorIndex = Math.Clamp(ci, 1, 255);

                // Dialog pro typ čáry
                string linetype = "Continuous";
                var linetypes = ClassManager.Instance.GetAvailableLinetypes(doc);
                var ltDialog = new InputDialog("Typ čáry",
                    $"Typ čáry [{string.Join(", ", linetypes)}]:");
                if (ltDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(ltDialog.InputText))
                    linetype = ltDialog.InputText;

                // Dialog pro tloušťku
                double lineweight = 0.25;
                var lwDialog = new InputDialog("Tloušťka čáry", "Tloušťka čáry v mm [0.25]:");
                if (lwDialog.ShowDialog() == true && double.TryParse(lwDialog.InputText,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var lw))
                    lineweight = lw;

                // Dialog pro průhlednost
                int transparency = 0;
                var trDialog = new InputDialog("Průhlednost", "Průhlednost 0-90% [0]:");
                if (trDialog.ShowDialog() == true && int.TryParse(trDialog.InputText, out var tr))
                    transparency = Math.Clamp(tr, 0, 90);

                ClassManager.Instance.CreateClass(nameDialog.InputText, doc,
                    colorIndex, linetype, lineweight, transparency);
            }
            catch (Exception ex)
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
                ClassManager.Instance.DeleteClass(cls, doc);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditClass_Click(object sender, RoutedEventArgs e)
        {
            if (ClassListView.SelectedItem is not VwClass cls) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Barva
            var colorDialog = new InputDialog("Barva", $"Index barvy ACI [{cls.ColorIndex}]:");
            if (colorDialog.ShowDialog() == true && int.TryParse(colorDialog.InputText, out var ci))
                cls.ColorIndex = Math.Clamp(ci, 1, 255);

            // Typ čáry
            var linetypes = ClassManager.Instance.GetAvailableLinetypes(doc);
            var ltDialog = new InputDialog("Typ čáry",
                $"Typ čáry [{cls.LinetypeName}]\nDostupné: {string.Join(", ", linetypes)}");
            if (ltDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(ltDialog.InputText))
                cls.LinetypeName = ltDialog.InputText;

            // Tloušťka
            var lwDialog = new InputDialog("Tloušťka", $"Tloušťka v mm [{cls.LineweightMm}]:");
            if (lwDialog.ShowDialog() == true && double.TryParse(lwDialog.InputText,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lw))
                cls.LineweightMm = lw;

            // Průhlednost
            var trDialog = new InputDialog("Průhlednost", $"Průhlednost 0-90% [{cls.Transparency}]:");
            if (trDialog.ShowDialog() == true && int.TryParse(trDialog.InputText, out var tr))
                cls.Transparency = Math.Clamp(tr, 0, 90);

            ClassManager.Instance.UpdateClassProperties(cls, doc);
        }

        private void SetActive_Click(object sender, RoutedEventArgs e)
        {
            if (ClassListView.SelectedItem is not VwClass cls) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            ClassManager.Instance.SetActive(cls, doc);
        }

        private void ClassListView_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Dvojklik = nastavit jako aktivní třídu
            if (ClassListView.SelectedItem is not VwClass cls) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            ClassManager.Instance.SetActive(cls, doc);
        }

        private void VisibilityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

            ClassManager.Instance.SetVisibility(cls, visibility, doc);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc != null)
                ClassManager.Instance.LoadFromDocument(doc);
        }

        private void ClassListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClassListView.SelectedItem is VwClass cls)
            {
                DetailPanel.Visibility = Visibility.Visible;
                DetailName.Text = cls.IsActive ? $"{cls.Name} (aktivní)" : cls.Name;
                DetailColorText.Text = $"ACI {cls.ColorIndex}";
                DetailLinetype.Text = cls.LinetypeName;
                DetailLineweight.Text = $"{cls.LineweightMm} mm";
                DetailTransparency.Text = $"{cls.Transparency}%";
                DetailColorSwatch.Fill = new SolidColorBrush(AciToApproxColor(cls.ColorIndex));
            }
            else
            {
                DetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        private static System.Windows.Media.Color AciToApproxColor(int aci) => aci switch
        {
            1 => Colors.Red,
            2 => Colors.Yellow,
            3 => Colors.Lime,
            4 => Colors.Cyan,
            5 => Colors.Blue,
            6 => Colors.Magenta,
            7 => Colors.White,
            8 => Colors.Gray,
            9 => Colors.LightGray,
            _ => Colors.White
        };
    }
}
