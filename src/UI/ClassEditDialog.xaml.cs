using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BricsLayerPlugin.UI
{
    public partial class ClassEditDialog : Window
    {
        public string ClassName { get; private set; } = string.Empty;
        public int ColorIndex { get; private set; } = 7;
        public string LinetypeName { get; private set; } = "Continuous";
        public double LineweightMm { get; private set; } = 0.25;
        public int Transparency { get; private set; }

        private static readonly double[] StandardLineweights =
        {
            0.00, 0.05, 0.09, 0.13, 0.15, 0.18, 0.20, 0.25, 0.30, 0.35,
            0.40, 0.50, 0.53, 0.60, 0.70, 0.80, 0.90, 1.00, 1.06, 1.20,
            1.40, 1.58, 2.00, 2.11
        };

        private static readonly (int Index, string Label, Color Color)[] QuickColors =
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
            (30, "30", Color.FromRgb(255, 127, 0)),
            (40, "40", Color.FromRgb(255, 191, 0)),
            (80, "80", Color.FromRgb(0, 127, 63)),
            (140, "140", Color.FromRgb(0, 63, 255)),
            (200, "200", Color.FromRgb(191, 0, 255)),
            (250, "250", Color.FromRgb(50, 50, 50)),
        };

        public ClassEditDialog(string name, int colorIndex, string linetype,
            double lineweight, int transparency, List<string> availableLinetypes,
            bool isNew = false)
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
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
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
            SetSelectedColor(colorIndex);

            ClassName = name;
            ColorIndex = colorIndex;
            LinetypeName = linetype;
            LineweightMm = lineweight;
            Transparency = transparency;

            if (isNew)
                NameBox.Focus();
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idx)
            {
                SetSelectedColor(idx);
                CustomColorBox.Text = idx.ToString();
            }
        }

        private void CustomColorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(CustomColorBox.Text, out int idx) && idx >= 1 && idx <= 255)
                SetSelectedColor(idx);
        }

        private void SetSelectedColor(int aci)
        {
            ColorIndex = Math.Clamp(aci, 1, 255);
            var color = AciToApproxColor(ColorIndex);
            ColorPreview.Fill = new SolidColorBrush(color);
            ColorIndexLabel.Text = $"ACI {ColorIndex}";

            // Highlight the matching quick button
            foreach (var child in ColorButtonPanel.Children)
            {
                if (child is Button btn)
                {
                    bool selected = btn.Tag is int t && t == ColorIndex;
                    btn.BorderThickness = new Thickness(selected ? 2 : 1);
                    btn.BorderBrush = new SolidColorBrush(selected
                        ? Color.FromRgb(0, 122, 204) : Color.FromRgb(85, 85, 85));
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

            // Parse lineweight from "0.25 mm" format
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

        internal static Color AciToApproxColor(int aci) => aci switch
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
            >= 10 and <= 19 => Color.FromRgb(255, (byte)(aci * 12), 0),
            >= 20 and <= 29 => Color.FromRgb(255, (byte)(127 + aci * 4), 0),
            >= 30 and <= 39 => Color.FromRgb(255, (byte)(170 + aci * 2), 0),
            >= 40 and <= 49 => Color.FromRgb(255, 255, (byte)(aci * 5)),
            >= 50 and <= 59 => Color.FromRgb((byte)(255 - aci * 3), 255, 0),
            >= 60 and <= 69 => Color.FromRgb((byte)(127 - aci), 255, 0),
            >= 70 and <= 79 => Color.FromRgb(0, 255, (byte)(aci * 3)),
            >= 80 and <= 89 => Color.FromRgb(0, 255, (byte)(127 + aci * 2)),
            >= 90 and <= 99 => Color.FromRgb(0, 255, 255),
            >= 100 and <= 109 => Color.FromRgb(0, (byte)(255 - aci * 2), 255),
            >= 110 and <= 119 => Color.FromRgb(0, (byte)(170 - aci), 255),
            >= 120 and <= 129 => Color.FromRgb(0, (byte)(100 - aci / 2), 255),
            >= 130 and <= 139 => Color.FromRgb(0, 0, 255),
            >= 140 and <= 149 => Color.FromRgb((byte)(aci - 60), 0, 255),
            >= 150 and <= 179 => Color.FromRgb((byte)(127 + aci / 3), 0, 255),
            >= 180 and <= 199 => Color.FromRgb(255, 0, (byte)(255 - aci)),
            >= 200 and <= 209 => Color.FromRgb(255, 0, 255),
            >= 210 and <= 229 => Color.FromRgb(255, 0, (byte)(200 - aci / 2)),
            >= 230 and <= 239 => Color.FromRgb((byte)(255 - aci / 3), (byte)(aci / 3), (byte)(aci / 3)),
            >= 240 and <= 249 => Color.FromRgb((byte)(200 - aci / 4), (byte)(200 - aci / 4), (byte)(200 - aci / 4)),
            250 => Color.FromRgb(50, 50, 50),
            251 => Color.FromRgb(80, 80, 80),
            252 => Color.FromRgb(105, 105, 105),
            253 => Color.FromRgb(130, 130, 130),
            254 => Color.FromRgb(190, 190, 190),
            255 => Colors.White,
            _ => Colors.White
        };
    }
}
