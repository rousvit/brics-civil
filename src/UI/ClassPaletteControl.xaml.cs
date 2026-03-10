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
            });
        }

        private void AddClass_Click(object sender, RoutedEventArgs e)
        {
            var nameDialog = new InputDialog("Nová třída", "Zadejte název třídy:");
            if (nameDialog.ShowDialog() != true) return;

            try
            {
                var cls = ClassManager.Instance.CreateClass(nameDialog.InputText);

                // Dialog pro barvu
                var colorDialog = new InputDialog("Barva", "Index barvy (1-255) [7]:");
                if (colorDialog.ShowDialog() == true && int.TryParse(colorDialog.InputText, out var ci))
                    cls.ColorIndex = Math.Clamp(ci, 1, 255);

                // Dialog pro typ čáry
                var ltDialog = new InputDialog("Typ čáry", "Typ čáry [Continuous]:");
                if (ltDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(ltDialog.InputText))
                    cls.LinetypeName = ltDialog.InputText;

                // Dialog pro tloušťku
                var lwDialog = new InputDialog("Tloušťka", "Tloušťka čáry v mm [0.25]:");
                if (lwDialog.ShowDialog() == true && double.TryParse(lwDialog.InputText, out var lw))
                    cls.LineweightMm = lw;

                RefreshList();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteClass_Click(object sender, RoutedEventArgs e)
        {
            if (ClassListView.SelectedItem is not VwClass cls) return;

            var result = MessageBox.Show(
                $"Opravdu smazat třídu '{cls.Name}'?\nObjekty budou nastaveny na ByLayer.",
                "Potvrdit smazání", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            ClassManager.Instance.DeleteClass(cls, doc);
        }

        private void EditClass_Click(object sender, RoutedEventArgs e)
        {
            if (ClassListView.SelectedItem is not VwClass cls) return;

            var colorDialog = new InputDialog("Barva", $"Nový index barvy [{cls.ColorIndex}]:");
            if (colorDialog.ShowDialog() == true && int.TryParse(colorDialog.InputText, out var ci))
                cls.ColorIndex = Math.Clamp(ci, 1, 255);

            var ltDialog = new InputDialog("Typ čáry", $"Nový typ čáry [{cls.LinetypeName}]:");
            if (ltDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(ltDialog.InputText))
                cls.LinetypeName = ltDialog.InputText;

            var lwDialog = new InputDialog("Tloušťka", $"Nová tloušťka [{cls.LineweightMm}]:");
            if (lwDialog.ShowDialog() == true && double.TryParse(lwDialog.InputText, out var lw))
                cls.LineweightMm = lw;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc != null)
                ClassManager.Instance.UpdateClass(cls, doc);
        }

        private void AssignClass_Click(object sender, RoutedEventArgs e)
        {
            if (ClassListView.SelectedItem is not VwClass cls) return;

            var doc = BcadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            ed.WriteMessage($"\nVyberte objekty pro přiřazení třídy '{cls.Name}'...");

            // Spustit výběr přes příkaz – uživatel musí spustit VW_CLASS_ASSIGN z příkazové řádky
            // pro plnou interaktivitu, protože SelectionSet z palety vyžaduje CommandFlags.UsePickSet
            MessageBox.Show(
                $"Pro přiřazení třídy '{cls.Name}' objektům:\n\n" +
                $"1. Vyberte objekty v kreslicí ploše\n" +
                $"2. Zadejte příkaz: VW_CLASS_ASSIGN\n" +
                $"3. Zadejte název třídy: {cls.Name}",
                "Přiřazení třídy", MessageBoxButton.OK, MessageBoxImage.Information);
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
                DetailName.Text = cls.Name;
                DetailColorText.Text = $"ACI {cls.ColorIndex}";
                DetailLinetype.Text = cls.LinetypeName;
                DetailLineweight.Text = $"{cls.LineweightMm} mm";

                // Přibližná barva pro ACI index
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
