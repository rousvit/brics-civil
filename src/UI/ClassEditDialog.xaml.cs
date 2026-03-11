using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Teigha.Colors;
using BcadColor = Teigha.Colors.Color;

namespace BricsLayerPlugin.UI
{
    public partial class ClassEditDialog : Window
    {
        public string ClassName { get; private set; } = string.Empty;
        public int ColorIndex { get; private set; } = 7;
        public bool IsTrueColor { get; private set; }
        public byte ColorRed { get; private set; }
        public byte ColorGreen { get; private set; }
        public byte ColorBlue { get; private set; }
        public string LinetypeName { get; private set; } = "Continuous";
        public double LineweightMm { get; private set; } = 0.25;
        public int Transparency { get; private set; }

        private static readonly double[] StandardLineweights =
        {
            0.00, 0.05, 0.09, 0.13, 0.15, 0.18, 0.20, 0.25, 0.30, 0.35,
            0.40, 0.50, 0.53, 0.60, 0.70, 0.80, 0.90, 1.00, 1.06, 1.20,
            1.40, 1.58, 2.00, 2.11
        };

        private static readonly (int Index, string Label, System.Windows.Media.Color Color)[] QuickColors =
        {
            (1, "1", Colors.Red),
            (2, "2", Colors.Yellow),
            (3, "3", Colors.Lime),
            (4, "4", Colors.Cyan),
            (5, "5", Colors.Blue),
            (6, "6", Colors.Magenta),
            (7, "7", Colors.White),
            (8, "8", Colors.Gray),
            (9, "9", Colors.LightGray),
        };

        public ClassEditDialog(string name, int colorIndex, string linetype,
            double lineweight, int transparency, List<string> availableLinetypes,
            bool isNew = false, bool isTrueColor = false,
            byte colorR = 0, byte colorG = 0, byte colorB = 0)
        {
            InitializeComponent();
            Title = isNew ? "Nová třída" : $"Upravit třídu - {name}";

            NameBox.Text = name;
            NameBox.IsEnabled = isNew;

            // Quick color buttons
            foreach (var (idx, label, color) in QuickColors)
            {
                var btn = new Button
                {
                    Width = 26, Height = 22,
                    Margin = new Thickness(1),
                    Background = new SolidColorBrush(color),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85)),
                    Tag = idx,
                    ToolTip = $"ACI {idx}",
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btn.Click += ColorButton_Click;
                ColorButtonPanel.Children.Add(btn);
            }

            // Linetypes
            foreach (var lt in availableLinetypes)
                LinetypeCombo.Items.Add(lt);
            LinetypeCombo.SelectedItem = linetype;
            if (LinetypeCombo.SelectedIndex < 0 && LinetypeCombo.Items.Count > 0)
                LinetypeCombo.SelectedIndex = 0;

            // Lineweights
            foreach (var lw in StandardLineweights)
                LineweightCombo.Items.Add($"{lw:F2} mm");

            int lwIdx = FindClosestLineweight(lineweight);
            LineweightCombo.SelectedIndex = lwIdx;

            // Transparency
            TransparencySlider.Value = transparency;
            TransparencyLabel.Text = $"{transparency} %";

            // Set initial color
            IsTrueColor = isTrueColor;
            ColorRed = colorR;
            ColorGreen = colorG;
            ColorBlue = colorB;
            ColorIndex = colorIndex;

            if (isTrueColor)
                SetTrueColor(colorR, colorG, colorB);
            else
                SetAciColor(colorIndex);

            ClassName = name;
            LinetypeName = linetype;
            LineweightMm = lineweight;
            Transparency = transparency;

            if (isNew)
                NameBox.Focus();
        }

        /// <summary>
        /// Otevře nativní BricsCAD ColorDialog (Index Color, True Color, Color Books).
        /// </summary>
        private void OpenColorDialog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Bricscad.Windows.ColorDialog();

                // Nastavit výchozí barvu
                if (IsTrueColor)
                    dlg.Color = BcadColor.FromRgb(ColorRed, ColorGreen, ColorBlue);
                else
                    dlg.Color = BcadColor.FromColorIndex(ColorMethod.ByAci, (short)ColorIndex);

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var selected = dlg.Color;

                    if (selected.IsByAci)
                    {
                        SetAciColor(selected.ColorIndex);
                    }
                    else
                    {
                        // True Color nebo Color Book -> RGB
                        SetTrueColor(selected.Red, selected.Green, selected.Blue);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"Nativní dialog barev není dostupný:\n{ex.Message}\n\nPoužijte rychlý výběr ACI nebo zadejte číslo.",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idx)
            {
                SetAciColor(idx);
                CustomColorBox.Text = idx.ToString();
            }
        }

        private void CustomColorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(CustomColorBox.Text, out int idx) && idx >= 1 && idx <= 255)
                SetAciColor(idx);
        }

        private void SetAciColor(int aci)
        {
            IsTrueColor = false;
            ColorIndex = Math.Clamp(aci, 1, 255);
            var color = AciToApproxColor(ColorIndex);
            ColorPreview.Fill = new SolidColorBrush(color);
            ColorIndexLabel.Text = $"ACI {ColorIndex}";
            TrueColorInfo.Visibility = Visibility.Collapsed;

            HighlightQuickButton(ColorIndex);
        }

        private void SetTrueColor(byte r, byte g, byte b)
        {
            IsTrueColor = true;
            ColorRed = r;
            ColorGreen = g;
            ColorBlue = b;
            var color = System.Windows.Media.Color.FromRgb(r, g, b);
            ColorPreview.Fill = new SolidColorBrush(color);
            ColorIndexLabel.Text = "True Color";
            TrueColorInfo.Text = $"RGB({r}, {g}, {b})";
            TrueColorInfo.Visibility = Visibility.Visible;

            HighlightQuickButton(-1); // deselect all
        }

        private void HighlightQuickButton(int selectedAci)
        {
            foreach (var child in ColorButtonPanel.Children)
            {
                if (child is Button btn)
                {
                    bool selected = btn.Tag is int t && t == selectedAci;
                    btn.BorderThickness = new Thickness(selected ? 2 : 1);
                    btn.BorderBrush = new SolidColorBrush(selected
                        ? System.Windows.Media.Color.FromRgb(0, 122, 204)
                        : System.Windows.Media.Color.FromRgb(85, 85, 85));
                }
            }
        }

        private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TransparencyLabel != null)
                TransparencyLabel.Text = $"{(int)TransparencySlider.Value} %";
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Název nesmí být prázdný.", "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ClassName = NameBox.Text.Trim();
            LinetypeName = LinetypeCombo.SelectedItem?.ToString() ?? "Continuous";
            Transparency = (int)TransparencySlider.Value;

            if (LineweightCombo.SelectedIndex >= 0 && LineweightCombo.SelectedIndex < StandardLineweights.Length)
                LineweightMm = StandardLineweights[LineweightCombo.SelectedIndex];

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private static int FindClosestLineweight(double target)
        {
            int idx = 0;
            double minDiff = double.MaxValue;
            for (int i = 0; i < StandardLineweights.Length; i++)
            {
                double diff = Math.Abs(StandardLineweights[i] - target);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    idx = i;
                }
            }
            return idx;
        }

        internal static System.Windows.Media.Color AciToApproxColor(int aci) => aci switch
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
            250 => System.Windows.Media.Color.FromRgb(50, 50, 50),
            251 => System.Windows.Media.Color.FromRgb(80, 80, 80),
            252 => System.Windows.Media.Color.FromRgb(105, 105, 105),
            253 => System.Windows.Media.Color.FromRgb(130, 130, 130),
            254 => System.Windows.Media.Color.FromRgb(190, 190, 190),
            255 => Colors.White,
            _ => Colors.White
        };

        /// <summary>
        /// Vrací barvu pro UI preview (WPF Color) z VwClass.
        /// </summary>
        internal static System.Windows.Media.Color GetDisplayColor(Models.VwClass cls)
        {
            if (cls.IsTrueColor)
                return System.Windows.Media.Color.FromRgb(cls.ColorRed, cls.ColorGreen, cls.ColorBlue);
            return AciToApproxColor(cls.ColorIndex);
        }
    }
}
