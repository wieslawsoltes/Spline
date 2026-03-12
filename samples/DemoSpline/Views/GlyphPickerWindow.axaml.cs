using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace DemoSpline.Views
{
    public partial class GlyphPickerWindow : Window
    {
        private readonly MainWindow _owner;
        private ComboBox _fontCombo = null!;
        private NumericUpDown _fontSize = null!;
        private TextBox _search = null!;
        private WrapPanel _wrap = null!;
        private Button _insertBtn = null!;
        private Button _closeBtn = null!;
        private Border? _selectedTile;
        private string? _selectedFamily;
        private int? _selectedGlyphIndex;
        private Geometry? _selectedGeometry;

        public GlyphPickerWindow(MainWindow owner)
        {
            _owner = owner;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            InitUi();
        }

        private void InitUi()
        {
            _fontCombo = this.FindControl<ComboBox>("FontCombo");
            _fontSize = this.FindControl<NumericUpDown>("FontSizeBox");
            _search = this.FindControl<TextBox>("SearchBox");
            _wrap = this.FindControl<WrapPanel>("GlyphWrap");
            _insertBtn = this.FindControl<Button>("InsertBtn");
            _closeBtn = this.FindControl<Button>("CloseBtn");

            var fams = TryEnumerateFonts();
            _fontCombo.ItemsSource = fams;
            if (fams.Count > 0) _fontCombo.SelectedIndex = 0;

            _fontCombo.SelectionChanged += (_, __) => RefreshGlyphs();
            _fontSize.ValueChanged += (_, __) => RefreshGlyphs();
            _search.PropertyChanged += (s, e) => { if (e.Property == TextBox.TextProperty) RefreshGlyphs(); };

            RefreshGlyphs();
        }

        private void OnInsertClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_selectedFamily != null && _selectedGlyphIndex.HasValue)
            {
                // Prefer passing the exact geometry used by the selected tile
                if (_selectedGeometry != null)
                {
                    _owner.InsertGlyphGeometry(_selectedGeometry);
                }
                else
                {
                    // Fallback: construct with same approach (GlyphRun.BuildGeometry)
                    double fs = (double)(_fontSize.Value ?? 120m);
                    if (FontManager.Current.TryGetGlyphTypeface(new Typeface(_selectedFamily), out var gt) && gt != null)
                    {
                        var gr = new GlyphRun(gt, fs, ReadOnlyMemory<char>.Empty, new ushort[] { (ushort)_selectedGlyphIndex.Value });
                        var geom = gr.BuildGeometry();
                        if (geom != null) _owner.InsertGlyphGeometry(geom);
                    }
                }
            }
        }

        private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }

        private static List<string> TryEnumerateFonts()
        {
            try
            {
                // Try Skia (cross-platform) to enumerate available fonts
                var fm = SkiaSharp.SKFontManager.Default;
                if (fm != null)
                {
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var fams = fm.FontFamilies;
                    if (fams != null)
                    {
                        foreach (var n in fams)
                        {
                            if (!string.IsNullOrWhiteSpace(n)) names.Add(n);
                        }
                    }
                    else
                    {
                        int cnt = fm.FontFamilyCount;
                        for (int i = 0; i < cnt; i++)
                        {
                            string? n = fm.GetFamilyName(i);
                            if (!string.IsNullOrWhiteSpace(n)) names.Add(n);
                        }
                    }
                    var list = names.OrderBy(n => n).ToList();
                    if (list.Count > 0) return list;
                }
            }
            catch { }
            return new List<string> { "Inter", "Arial", "Helvetica", "Times New Roman", "Courier New", "System UI" };
        }

        private void RefreshGlyphs()
        {
            if (_fontCombo.SelectedItem is not string familyName) return;
            double fontSize = (double)(_fontSize.Value ?? 120m);

            _wrap.Children.Clear();
            _selectedTile = null;
            _selectedFamily = null;
            _selectedGlyphIndex = null;
            _insertBtn.IsEnabled = false;
            _selectedGeometry = null;

            var typeface = new Typeface(familyName);
            if (!FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface) || glyphTypeface is null)
                return;

            // Basic range; inspect supported glyphs via total glyph count
            int glyphCount = glyphTypeface.GlyphCount;
            string? filter = _search.Text;

            for (int gi = 0; gi < glyphCount; gi++)
            {
                // Optional filter by codepoint name is non-trivial; for now filter by index text match
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    bool match = gi.ToString().Contains(filter!, StringComparison.OrdinalIgnoreCase);
                    if (!match) continue;
                }

                // Render glyph outline via GlyphRun geometry
                var glyphRun = new GlyphRun(glyphTypeface, fontSize, ReadOnlyMemory<char>.Empty, new ushort[] { (ushort)gi });
                var geom = glyphRun.BuildGeometry();
                if (geom is null) continue;

                var border = new Border { Width = 120, Height = 140, Padding = new Thickness(6), Margin = new Thickness(4), BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1), Background = Brushes.Transparent, Tag = gi };
                var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
                var path = new Path { Data = geom, Stroke = Brushes.Black, StrokeThickness = 1, Stretch = Stretch.Uniform, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
                var lbl = new TextBlock { Text = GetGlyphLabel(glyphTypeface, gi), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(0,4,0,0) };
                Grid.SetRow(path, 0);
                Grid.SetRow(lbl, 1);
                grid.Children.Add(path);
                grid.Children.Add(lbl);
                border.Child = grid;
                border.PointerPressed += (_, __) => { SelectTile(border, familyName, gi); };
                grid.PointerPressed += (_, __) => { SelectTile(border, familyName, gi); };
                path.PointerPressed += (_, __) => { SelectTile(border, familyName, gi); };
                lbl.PointerPressed += (_, __) => { SelectTile(border, familyName, gi); };
                border.DoubleTapped += (_, __) =>
                {
                    SelectTile(border, familyName, gi);
                    if (_selectedGeometry != null)
                    {
                        _owner.InsertGlyphGeometry(_selectedGeometry);
                    }
                };
                _wrap.Children.Add(border);
            }
        }

        private string GetGlyphLabel(IGlyphTypeface glyphTypeface, int gi) => $"#{gi}";

        private void SelectTile(Border tile, string familyName, int glyphIndex)
        {
            if (_selectedTile != null)
            {
                _selectedTile.BorderBrush = Brushes.LightGray;
                _selectedTile.Background = Brushes.Transparent;
            }
            _selectedTile = tile;
            _selectedFamily = familyName;
            _selectedGlyphIndex = glyphIndex;
            _selectedGeometry = null;
            if (tile.Child is Grid g)
            {
                var p = g.Children.OfType<Path>().FirstOrDefault();
                if (p != null)
                {
                    _selectedGeometry = p.Data;
                }
            }
            _insertBtn.IsEnabled = true;
            tile.BorderBrush = Brushes.DeepSkyBlue;
            tile.Background = new SolidColorBrush(Color.FromArgb(24, 30, 144, 255));
        }
    }
}

