using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DemoSpline.Models;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using Spline.Core;
using SplinePath = Spline.Core.Spline;

namespace DemoSpline.Views;

public partial class MainWindow : Window
{
    private sealed class SubpathData
    {
        public readonly List<SplinePath.CP> Knots = new();
        public bool Closed;
    }

    private readonly List<SubpathData> _subpaths = new();
    private int _activeSubpath = 0;
    private List<SplinePath.CP> _knots => EnsureActiveSubpath().Knots;
    private bool _isClosed { get => EnsureActiveSubpath().Closed; set => EnsureActiveSubpath().Closed = value; }
    private readonly List<BezierPath> _paths = new();
    private sealed class GlyphOverlay
    {
        public required Geometry Geom { get; init; }
        public double Scale { get; set; }
        public double Ox { get; set; }
        public double Oy { get; set; }
        public bool Selected { get; set; }
        public bool NeedsAutoFit { get; set; }
    }
    private readonly List<GlyphOverlay> _glyphs = new();
    private bool _showGrid = true;
    private Point? _lastPointerContent;
    private bool _dragging;
    private SplinePath.CP? _activeKnot;
    private readonly HashSet<SplinePath.CP> _selection = new();
    private bool _shiftOnDrag;

    // Tangent handle drag state
    private bool _dragTan;
    private SplinePath.CP? _tanKnot;
    private bool _tanIsRight;
    private Point _tanInitPt;
    private bool _creating;
    private Point _initPt;

    // Tangent rendering sizes
    private const double TanR1 = 5;   // convert to corner when creating if inside
    private const double TanR2 = 15;  // radius of drawn marker
    private const double TanR3 = 45;  // outside remove explicit tangent

    private Settings _settings = Settings.Load();
    private MenuItem? _showGridMenuItem;
    private MenuItem? _rawCubicMenuItem;
    private MenuItem? _editToolMenuItem;
    private MenuItem? _freehandToolMenuItem;
    private MenuItem? _deletePointToolMenuItem;
    private MenuItem? _glyphEditToolMenuItem;
    private Point? _pointerPos;
	private bool _svgHover;
    private bool _useRawCubic;

    // View transform state moved into PanZoomHost

    private enum ToolMode
    {
        EditSpline,
        FreehandTrace,
        DeletePoint,
        GlyphEdit
    }

    private ToolMode _tool = ToolMode.EditSpline;

    // Freehand trace state
    private bool _freehandActive;
    private readonly List<Point> _freehandPoints = new();
    private bool _freehandSession;

    public MainWindow()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (_, __) =>
        {
            _showGrid = _settings.ShowGrid;
            _showGridMenuItem = this.FindControl<MenuItem>("ShowGridMenu");
            if (_showGridMenuItem != null)
            {
                _showGridMenuItem.IsChecked = _showGrid;
            }
            _rawCubicMenuItem = this.FindControl<MenuItem>("RawCubicMenu");
            if (_rawCubicMenuItem != null)
            {
                _rawCubicMenuItem.IsChecked = _useRawCubic;
            }
            _editToolMenuItem = this.FindControl<MenuItem>("EditToolMenu");
            _freehandToolMenuItem = this.FindControl<MenuItem>("FreehandToolMenu");
            _deletePointToolMenuItem = this.FindControl<MenuItem>("DeletePointToolMenu");
            _glyphEditToolMenuItem = this.FindControl<MenuItem>("GlyphEditToolMenu");
            UpdateToolMenuChecks();
            RenderAll();
        };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // Attach events after the window is opened to ensure the canvas is realized
        EditorCanvas.PointerPressed += OnPointerPressed;
        EditorCanvas.PointerMoved += OnPointerMoved;
        EditorCanvas.PointerReleased += OnPointerReleased;
        EditorCanvas.PointerEntered += (_, ev) => { _pointerPos = ev.GetPosition(EditorCanvas); _svgHover = true; RenderAll(); };
        EditorCanvas.PointerExited += (_, __) => { _pointerPos = null; _svgHover = false; RenderAll(); };
        // Re-render on size changes of the viewport
        Viewport.GetObservable(BoundsProperty).Subscribe(new Avalonia.Reactive.AnonymousObserver<Rect>(_ => { RenderAll(); }));
        this.KeyDown += OnKeyDown;
        RenderAll();
    }

    private async void OnLoadJson(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (this.StorageProvider is null) return;
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Open JSON",
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("JSON") { Patterns = new List<string> { "*.json" } }
            }
        });
        if (files == null || files.Count == 0) return;
        try
        {
            await using var s = await files[0].OpenReadAsync();
            using var r = new StreamReader(s);
            string json = await r.ReadToEndAsync();
            Deserialize(json);
        }
        catch (Exception ex)
        {
            await MessageBox($"Failed to load JSON: {ex.Message}");
        }
    }

    private async void OnSaveJson(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (this.StorageProvider is null) return;
        var result = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save JSON",
            SuggestedFileName = "spline.json",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("JSON") { Patterns = new List<string> { "*.json" } }
            }
        });
        if (result is null) return;
        try
        {
            string json = Serialize();
            await using var s = await result.OpenWriteAsync();
            using var w = new StreamWriter(s);
            await w.WriteAsync(json);
        }
        catch (Exception ex)
        {
            await MessageBox($"Failed to save JSON: {ex.Message}");
        }
    }

    private void OnToggleGrid(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _showGrid = !_showGrid;
        if (_showGridMenuItem != null) _showGridMenuItem.IsChecked = _showGrid;
        _settings.ShowGrid = _showGrid;
        _settings.Save();
        RenderAll();
    }

    private void OnOpenTuner(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var wnd = new TunerWindow();
        wnd.Show(this);
    }

    private void OnNewDrawing(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _subpaths.Clear();
        _paths.Clear();
        _glyphs.Clear();
        _activeSubpath = 0;
        _selection.Clear();
        _activeKnot = null;
        _dragging = false;
        _dragTan = false;
        _creating = false;
        _freehandActive = false;
        _freehandPoints.Clear();
        _freehandSession = false;
        _tool = ToolMode.EditSpline;
        ZoomHost.ResetMatrix(); _lastPointerContent = null;
        UpdateToolMenuChecks();
        RenderAll();
    }

    private void OnOpenHelp(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var wnd = new HelpWindow();
        wnd.Show(this);
    }

    private void OnOpenGlyphPicker(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var wnd = new GlyphPickerWindow(this);
        wnd.Show(this);
    }

    public void InsertGlyphGeometry(Geometry geom)
    {
        // Initial placement to fit canvas with padding
        double cw = GlyphCanvas.Bounds.Width;
        double ch = GlyphCanvas.Bounds.Height;
        var gb = geom.Bounds;
        if (gb.Width <= 0 || gb.Height <= 0) return;
        if (cw < 2 || ch < 2)
        {
            // Defer fit until canvas has size; place at origin-relative
            double s0 = 1.0;
            double ox0 = -gb.X;
            double oy0 = -gb.Y;
            _glyphs.Add(new GlyphOverlay { Geom = geom, Scale = s0, Ox = ox0, Oy = oy0, Selected = false, NeedsAutoFit = true });
        }
        else
        {
            // Center glyph bounds within canvas with padding, accounting for bounds origin and scale
            double extraPad = 20.0;
            double pad = extraPad + 0.5;
            double innerW = Math.Max(cw - 2 * pad, 1);
            double innerH = Math.Max(ch - 2 * pad, 1);
            double sx = innerW / gb.Width;
            double sy = innerH / gb.Height;
            double s = Math.Min(sx, sy);
            if (double.IsInfinity(s) || double.IsNaN(s) || s <= 0) s = 1.0;
            // Empirical correction: bounds-origin-centered glyphs appear with center at top-left, shift by half extents
            double ox = (cw - s * gb.Width) * 0.5 - s * gb.X + 0.5 * s * gb.Width;
            double oy = (ch - s * gb.Height) * 0.5 - s * gb.Y + 0.5 * s * gb.Height;
            _glyphs.Add(new GlyphOverlay { Geom = geom, Scale = s, Ox = ox, Oy = oy, Selected = false, NeedsAutoFit = false });
        }
        RenderAll();
    }

    // Keep method for potential future use, but prefer passing exact geometry from picker tiles
    public void InsertGlyph(string family, int glyphIndex, double fontSize)
    {
        if (!FontManager.Current.TryGetGlyphTypeface(new Typeface(family), out var glyphTypeface) || glyphTypeface is null)
            return;
        var geom = new GlyphRun(glyphTypeface, fontSize, ReadOnlyMemory<char>.Empty, new ushort[] { (ushort)glyphIndex }).BuildGeometry();
        if (geom is null) return;
        InsertGlyphGeometry(geom);
        RenderAll();
    }

    private void OnToggleRawCubic(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _useRawCubic = !_useRawCubic;
        if (_rawCubicMenuItem != null) _rawCubicMenuItem.IsChecked = _useRawCubic;
        RenderAll();
    }

    private void OnReduceKnots(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_subpaths.All(sp => sp.Knots.Count <= 2)) return;
        // Ask user for tolerance (in pixels)
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            double tol = 2.0;
            try
            {
                var dlg = new Window
                {
                    Width = 320,
                    Height = 160,
                    Title = "Reduce Knots",
                };
                var tb = new TextBox { Text = tol.ToString(CultureInfo.InvariantCulture), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, Margin = new Thickness(0, 6, 0, 0) };
                var ok = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
                var sp = new StackPanel { Margin = new Thickness(10) };
                sp.Children.Add(new TextBlock { Text = "Max deviation (px):" });
                sp.Children.Add(tb);
                sp.Children.Add(ok);
                dlg.Content = sp;
                ok.Click += (_, __) => dlg.Close();
                await dlg.ShowDialog(this);
                if (!double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out tol)) tol = 2.0;
            }
            catch { }

            ReduceKnotsByToleranceAll(tol);
            RenderAll();
        });
    }

    private async void OnFairCurvature(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Dialog for parameters
        double maxMovePx = 4.0;
        double curvatureWeight = 1.0;
        double fitTolerance = 2.0;
        try
        {
            var dlg = new Window { Width = 360, Height = 220, Title = "Fair Curvature" };
            var sp = new StackPanel { Margin = new Thickness(10) };
            var tbMove = new TextBox { Text = maxMovePx.ToString(CultureInfo.InvariantCulture) };
            var tbCurv = new TextBox { Text = curvatureWeight.ToString(CultureInfo.InvariantCulture) };
            var tbTol = new TextBox { Text = fitTolerance.ToString(CultureInfo.InvariantCulture) };
            sp.Children.Add(new TextBlock { Text = "Max move (px):" });
            sp.Children.Add(tbMove);
            sp.Children.Add(new TextBlock { Text = "Curvature weight:" });
            sp.Children.Add(tbCurv);
            sp.Children.Add(new TextBlock { Text = "Fit tolerance (px):" });
            sp.Children.Add(tbTol);
            var ok = new Button { Content = "OK", IsDefault = true, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Thickness(0,10,0,0) };
            sp.Children.Add(ok);
            dlg.Content = sp;
            ok.Click += (_, __) => dlg.Close();
            await dlg.ShowDialog(this);
            double.TryParse(tbMove.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out maxMovePx);
            double.TryParse(tbCurv.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out curvatureWeight);
            double.TryParse(tbTol.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out fitTolerance);
        }
        catch { }

        FairCurvatureAll(maxMovePx, curvatureWeight, fitTolerance);
        RenderAll();
    }

    private async void OnStraightenSpans(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        double straightnessEpsDeg = 6.0; // angle threshold for near-collinearity
        double maxMovePx = 4.0;
        double fitTolerance = 2.0;
        try
        {
            var dlg = new Window { Width = 360, Height = 220, Title = "Straighten Spans" };
            var sp = new StackPanel { Margin = new Thickness(10) };
            var tbEps = new TextBox { Text = straightnessEpsDeg.ToString(CultureInfo.InvariantCulture) };
            var tbMove = new TextBox { Text = maxMovePx.ToString(CultureInfo.InvariantCulture) };
            var tbTol = new TextBox { Text = fitTolerance.ToString(CultureInfo.InvariantCulture) };
            sp.Children.Add(new TextBlock { Text = "Straightness ε (deg):" });
            sp.Children.Add(tbEps);
            sp.Children.Add(new TextBlock { Text = "Max move (px):" });
            sp.Children.Add(tbMove);
            sp.Children.Add(new TextBlock { Text = "Fit tolerance (px):" });
            sp.Children.Add(tbTol);
            var ok = new Button { Content = "OK", IsDefault = true, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Thickness(0,10,0,0) };
            sp.Children.Add(ok);
            dlg.Content = sp;
            ok.Click += (_, __) => dlg.Close();
            await dlg.ShowDialog(this);
            double.TryParse(tbEps.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out straightnessEpsDeg);
            double.TryParse(tbMove.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out maxMovePx);
            double.TryParse(tbTol.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out fitTolerance);
        }
        catch { }

        StraightenSpansAll(straightnessEpsDeg * Math.PI / 180.0, maxMovePx, fitTolerance);
        RenderAll();
    }

    private void FairCurvatureAll(double maxMovePx, double curvatureWeight, double fitTolerance)
    {
        if (_subpaths.Count == 0) return;
        foreach (var sp in _subpaths)
        {
            if (sp.Knots.Count < 3) continue;
            // Work on a copy for deviation checks
            var original = sp.Knots.Select(k => new SplinePath.CP(new Vec2(k.Pt.X, k.Pt.Y), k.Ty, k.LTh, k.RTh)).ToList();

            int iterations = 2;
            for (int it = 0; it < iterations; it++)
            {
                for (int i = 1; i < sp.Knots.Count - 1; i++)
                {
                    var k = sp.Knots[i];
                    if (k.Ty == "corner") continue;
                    var prev = sp.Knots[i - 1].Pt;
                    var next = sp.Knots[i + 1].Pt;
                    // Laplacian target
                    var target = new Vec2((prev.X + next.X) * 0.5, (prev.Y + next.Y) * 0.5);
                    var cur = k.Pt;
                    var delta = new Vec2(target.X - cur.X, target.Y - cur.Y);
                    double len = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                    if (len < 1e-6) continue;
                    double step = Math.Min(maxMovePx, len);
                    var candidate = new Vec2(cur.X + delta.X / len * step, cur.Y + delta.Y / len * step);

                    // Evaluate deviation and simple curvature proxy improvement
                    bool accept = ImprovesFairnessWithTolerance(original, sp.Knots, i, candidate, fitTolerance, curvatureWeight);
                    if (accept) k.Pt = candidate;
                }
            }
        }
    }

    private void StraightenSpansAll(double straightnessEpsRad, double maxMovePx, double fitTolerance)
    {
        foreach (var sp in _subpaths)
        {
            if (sp.Knots.Count < 3) continue;
            var original = sp.Knots.Select(k => new SplinePath.CP(new Vec2(k.Pt.X, k.Pt.Y), k.Ty, k.LTh, k.RTh)).ToList();
            int i = 0;
            while (i + 2 < sp.Knots.Count)
            {
                var a = sp.Knots[i]; var b = sp.Knots[i + 1]; var c = sp.Knots[i + 2];
                if (a.Ty == "corner" || b.Ty == "corner" || c.Ty == "corner") { i++; continue; }
                double abx = b.Pt.X - a.Pt.X, aby = b.Pt.Y - a.Pt.Y;
                double bcx = c.Pt.X - b.Pt.X, bcy = c.Pt.Y - b.Pt.Y;
                double dot = abx * bcx + aby * bcy;
                double la = Math.Sqrt(abx * abx + aby * aby);
                double lb = Math.Sqrt(bcx * bcx + bcy * bcy);
                if (la < 1e-6 || lb < 1e-6) { i++; continue; }
                double cosang = Math.Clamp(dot / (la * lb), -1, 1);
                double ang = Math.Acos(cosang);
                if (ang < straightnessEpsRad)
                {
                    // Project b to line ac with clamped maxMovePx
                    var acx = c.Pt.X - a.Pt.X; var acy = c.Pt.Y - a.Pt.Y;
                    double acl2 = acx * acx + acy * acy; if (acl2 < 1e-12) { i++; continue; }
                    double t = ((b.Pt.X - a.Pt.X) * acx + (b.Pt.Y - a.Pt.Y) * acy) / acl2;
                    t = Math.Clamp(t, 0, 1);
                    var proj = new Vec2(a.Pt.X + t * acx, a.Pt.Y + t * acy);
                    var delta = new Vec2(proj.X - b.Pt.X, proj.Y - b.Pt.Y);
                    double len = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
                    if (len > 1e-6)
                    {
                        double step = Math.Min(len, maxMovePx);
                        var candidate = new Vec2(b.Pt.X + delta.X / len * step, b.Pt.Y + delta.Y / len * step);
                        bool accept = ImprovesFairnessWithTolerance(original, sp.Knots, i + 1, candidate, fitTolerance, 0.0);
                        if (accept) b.Pt = candidate;
                    }
                }
                i++;
            }
        }
    }

    private bool ImprovesFairnessWithTolerance(List<SplinePath.CP> originalCopy, List<SplinePath.CP> current, int index, Vec2 candidate, double fitTolerance, double curvatureWeight)
    {
        // Build temp knot list with candidate
        var temp = current.Select((k, idx) => new SplinePath.CP(new Vec2(idx == index ? candidate.X : k.Pt.X, idx == index ? candidate.Y : k.Pt.Y), k.Ty, k.LTh, k.RTh)).ToList();
        // Evaluate geometric deviation w.r.t original path
        var origSpline = new SplinePath(originalCopy, false);
        origSpline.Solve();
        origSpline.ComputeCurvatureBlending();
        var origPath = origSpline.Render(_useRawCubic);

        var newSpline = new SplinePath(temp, false);
        newSpline.Solve();
        newSpline.ComputeCurvatureBlending();
        var newPath = newSpline.Render(_useRawCubic);

        // Sample original knots as witnesses
        double maxDev = 0;
        for (int i = 0; i < originalCopy.Count; i++)
        {
            var p = originalCopy[i].Pt;
            var ht = newPath.HitTest(p.X, p.Y);
            if (ht.BestDist > maxDev) maxDev = ht.BestDist;
            if (maxDev > fitTolerance) break;
        }
        if (maxDev > fitTolerance) return false;

        if (curvatureWeight <= 0) return true;
        // Simple curvature proxy: sum of turning angles deviation
        double CurvProxy(List<SplinePath.CP> list)
        {
            double s = 0;
            for (int i = 1; i + 1 < list.Count; i++)
            {
                var a = list[i - 1].Pt; var b = list[i].Pt; var c = list[i + 1].Pt;
                double abx = b.X - a.X, aby = b.Y - a.Y;
                double bcx = c.X - b.X, bcy = c.Y - b.Y;
                double la = Math.Sqrt(abx * abx + aby * aby);
                double lb = Math.Sqrt(bcx * bcx + bcy * bcy);
                if (la < 1e-6 || lb < 1e-6) continue;
                double cosang = Math.Clamp((abx * bcx + aby * bcy) / (la * lb), -1, 1);
                double ang = Math.Acos(cosang);
                s += ang * ang;
            }
            return s;
        }
        double curCurv = CurvProxy(current);
        double newCurv = CurvProxy(temp);
        return newCurv <= curCurv;
    }

    private void ReduceKnotsByToleranceAll(double tolerance)
    {
        foreach (var sp in _subpaths)
        {
            if (sp.Knots.Count <= 2) continue;
            bool changed;
            int guard = 0;
            do
            {
                changed = false;
                guard++;
                if (guard > 1000) break;

                for (int i = 1; i < sp.Knots.Count - 1; i++)
                {
                    var backup = sp.Knots[i];
                    sp.Knots.RemoveAt(i);

                    var spline = new SplinePath(new List<SplinePath.CP>(sp.Knots), sp.Closed);
                    spline.Solve();
                    spline.ComputeCurvatureBlending();
                    var path = spline.Render(_useRawCubic);

                    double maxErr = 0;
                    for (int k = 0; k < sp.Knots.Count - 1; k++)
                    {
                        var a = sp.Knots[k].Pt;
                        var b = sp.Knots[k + 1].Pt;
                        int n = 8;
                        for (int s = 0; s <= n; s++)
                        {
                            double tt = (double)s / n;
                            double x = a.X + tt * (b.X - a.X);
                            double y = a.Y + tt * (b.Y - a.Y);
                            var ht = path.HitTest(x, y);
                            if (ht.BestDist > maxErr) maxErr = ht.BestDist;
                            if (maxErr > tolerance) break;
                        }
                        if (maxErr > tolerance) break;
                    }

                    if (maxErr <= tolerance)
                    {
                        changed = true;
                        i--;
                    }
                    else
                    {
                        sp.Knots.Insert(i, backup);
                    }
                }
            } while (changed);

            if (sp.Knots.Count < 3) sp.Closed = false;
        }
    }

    private void UpdateToolMenuChecks()
    {
        if (_editToolMenuItem != null) _editToolMenuItem.IsChecked = _tool == ToolMode.EditSpline;
        if (_freehandToolMenuItem != null) _freehandToolMenuItem.IsChecked = _tool == ToolMode.FreehandTrace;
        if (_deletePointToolMenuItem != null) _deletePointToolMenuItem.IsChecked = _tool == ToolMode.DeletePoint;
    }

    private void OnSelectEditTool(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _tool = ToolMode.EditSpline;
        _freehandActive = false;
        _freehandPoints.Clear();
        UpdateToolMenuChecks();
        RenderAll();
    }

    private void OnSelectFreehandTool(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _tool = ToolMode.FreehandTrace;
        _dragging = false;
        _dragTan = false;
        _activeKnot = null;
        _creating = false;
        if (_tool == ToolMode.GlyphEdit)
        {
            // Stop glyph drag on release
            _dragging = false;
        }
        _freehandActive = false;
        _freehandPoints.Clear();
        _freehandSession = false;
        UpdateToolMenuChecks();
        RenderAll();
    }

    private void OnSelectDeletePointTool(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _tool = ToolMode.DeletePoint;
        _dragging = false;
        _dragTan = false;
        _activeKnot = null;
        _creating = false;
        _freehandActive = false;
        _freehandPoints.Clear();
        _freehandSession = false;
        UpdateToolMenuChecks();
        RenderAll();
    }

    private void OnSelectGlyphEditTool(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _tool = ToolMode.GlyphEdit;
        _dragging = false;
        _dragTan = false;
        _activeKnot = null;
        _creating = false;
        _freehandActive = false;
        _freehandPoints.Clear();
        _freehandSession = false;
        foreach (var g in _glyphs) g.Selected = false;
        UpdateToolMenuChecks();
        RenderAll();
    }


    private void OnDeletePoint(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Mirror menu-delete behavior from demo
        foreach (var sp in _subpaths)
        {
            for (int i = 0; i < sp.Knots.Count; i++)
            {
                if (_selection.Contains(sp.Knots[i]))
                {
                    sp.Knots.RemoveAt(i);
                    i--;
                }
            }
            if (sp.Knots.Count < 3) sp.Closed = false;
        }
        _selection.Clear();
        // Clear all state variables that might reference deleted knots
        _activeKnot = null;
        _dragging = false;
        _dragTan = false;
        RenderAll();
    }

    // Zoom menu handlers
    private void OnZoomReset(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ZoomHost.ResetMatrix();
        RenderAll();
    }

    private void OnZoomIn(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Programmatic zoom-in via control API can be added; wheel zoom works by default
        return;
    }

    private void OnZoomOut(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Programmatic zoom-out via control API can be added; wheel zoom works by default
        return;
    }

    private void OnZoomFit(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FitContentToView();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pv = e.GetPosition(EditorCanvas);
        var p = pv;
        _pointerPos = pv;
        _lastPointerContent = p;
        _dragTan = false;
        _shiftOnDrag = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var pp = e.GetCurrentPoint(EditorCanvas);

        if (_tool == ToolMode.FreehandTrace)
        {
            // Right click finishes the freehand session
            if (pp.Properties.IsRightButtonPressed)
            {
                _freehandActive = false;
                _freehandPoints.Clear();
                _freehandSession = false;
                _tool = ToolMode.EditSpline;
                UpdateToolMenuChecks();
                RenderAll();
                return;
            }

            // Left press starts a new figure stroke
            _freehandActive = true;
            _freehandPoints.Clear();
            _freehandPoints.Add(p);
            _freehandSession = true;
            RenderAll();
            return;
        }

        if (_tool == ToolMode.DeletePoint)
        {
            // hit test knots across all subpaths and delete on click
            SplinePath.CP? hitKnotDel = null;
            int hitSubDel = _activeSubpath;
            double tol = 6.0;
            for (int si = 0; si < _subpaths.Count; si++)
            {
                foreach (var k in _subpaths[si].Knots)
                {
                    if (Math.Abs(k.Pt.X - p.X) < tol && Math.Abs(k.Pt.Y - p.Y) < tol)
                    {
                        hitKnotDel = k;
                        hitSubDel = si;
                        break;
                    }
                }
                if (hitKnotDel != null) break;
            }
            if (hitKnotDel != null)
            {
                _activeSubpath = hitSubDel;
                _subpaths[hitSubDel].Knots.Remove(hitKnotDel);
                if (_subpaths[hitSubDel].Knots.Count < 3) _subpaths[hitSubDel].Closed = false;
                _selection.Remove(hitKnotDel);
                // Clear stale references that might point to the deleted knot
                if (_activeKnot == hitKnotDel)
                {
                    _activeKnot = null;
                    _dragging = false;
                    _dragTan = false;
                }
                RenderAll();
                return;
            }
            // No hit: do nothing in delete mode
            return;
        }

        // In EditSpline tool, right-click deletes a point under the cursor
        if (_tool == ToolMode.EditSpline && pp.Properties.IsRightButtonPressed)
        {
            SplinePath.CP? hitKnotDel = null;
            int hitSubDel = _activeSubpath;
            for (int si = 0; si < _subpaths.Count; si++)
            {
                foreach (var k in _subpaths[si].Knots)
                {
                    if (Math.Abs(k.Pt.X - p.X) < 6 && Math.Abs(k.Pt.Y - p.Y) < 6)
                    {
                        hitKnotDel = k;
                        hitSubDel = si;
                        break;
                    }
                }
                if (hitKnotDel != null) break;
            }
            if (hitKnotDel != null)
            {
                _activeSubpath = hitSubDel;
                _subpaths[hitSubDel].Knots.Remove(hitKnotDel);
                if (_subpaths[hitSubDel].Knots.Count < 3) _subpaths[hitSubDel].Closed = false;
                _selection.Remove(hitKnotDel);
                // Clear stale references that might point to the deleted knot
                if (_activeKnot == hitKnotDel)
                {
                    _activeKnot = null;
                    _dragging = false;
                    _dragTan = false;
                }
                RenderAll();
            }
            return;
        }

        if (_tool == ToolMode.GlyphEdit)
        {
            // Toggle selection or start dragging selected glyph
            // Hit test glyphs by inverse transform of their scale/translate
            for (int gi = _glyphs.Count - 1; gi >= 0; gi--)
            {
                var g = _glyphs[gi];
                var b = g.Geom.Bounds;
                // inverse transform
                double ix = (p.X - g.Ox) / g.Scale;
                double iy = (p.Y - g.Oy) / g.Scale;
                if (ix >= b.X && ix <= b.X + b.Width && iy >= b.Y && iy <= b.Y + b.Height)
                {
                    // Select this glyph exclusively
                    foreach (var gg in _glyphs) gg.Selected = false;
                    g.Selected = true;
                    _lastPointerContent = p;
                    _dragging = true; // reuse dragging flag to move glyph in OnPointerMoved
                    RenderAll();
                    return;
                }
            }
            // Clicked empty: clear glyph selection
            foreach (var gg in _glyphs) gg.Selected = false;
            RenderAll();
            return;
        }

        // First hit test tangent handles created in RenderSel via Tag
        if (e.Source is Shape sh && sh.Tag is TanHandleTag tag)
        {
            _dragTan = true;
            _tanKnot = tag.Knot;
            _tanIsRight = tag.IsRight;
            _tanInitPt = new Point(tag.Knot.Pt.X, tag.Knot.Pt.Y);
            UpdateTan(tag.Knot, p, e.KeyModifiers);
            _selection.Clear();
            _selection.Add(tag.Knot);
            RenderAll();
            return;
        }

        // hit test knots across all subpaths
        SplinePath.CP? hitKnot = null;
        int hitSubpathIndex = _activeSubpath;
        double tol2 = 6.0;
        for (int si = 0; si < _subpaths.Count; si++)
        {
            foreach (var k in _subpaths[si].Knots)
            {
                if (Math.Abs(k.Pt.X - p.X) < tol2 && Math.Abs(k.Pt.Y - p.Y) < tol2)
                {
                    hitKnot = k;
                    hitSubpathIndex = si;
                    break;
                }
            }
            if (hitKnot != null) break;
        }

        if (hitKnot != null)
        {
            // switch active subpath if needed
            _activeSubpath = hitSubpathIndex;
            if (e.ClickCount > 1)
            {
                // Toggle ty
                hitKnot.Ty = hitKnot.Ty == "corner" ? "smooth" : "corner";
                hitKnot.LTh = null;
                hitKnot.RTh = null;
            }
            if (_selection.Count == 1 && _selection.Contains(_knots.Last()) && _knots.Count >= 3 && hitKnot == _knots.First())
            {
                _isClosed = true;
            }
            if (_shiftOnDrag)
            {
                if (_selection.Contains(hitKnot)) _selection.Remove(hitKnot); else _selection.Add(hitKnot);
            }
            else
            {
                _selection.Clear();
                _selection.Add(hitKnot);
            }
            _activeKnot = hitKnot;
            _dragging = true;
            RenderAll();
            return;
        }

        // Empty space: possibly subdivide segment by hit-testing bezier path
        int insertIx = _knots.Count;
        int targetSubpath = _activeSubpath;
        bool makeSmooth = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        if (_paths.Count > 0)
        {
            double best = double.MaxValue;
            int bestMark = -1;
            int bestSub = -1;
            for (int si = 0; si < _paths.Count; si++)
            {
                var ht = _paths[si].HitTest(p.X, p.Y);
                if (ht.BestDist < best && ht.BestMark.HasValue)
                {
                    best = ht.BestDist;
                    bestMark = ht.BestMark.Value;
                    bestSub = si;
                }
            }
            double hitTol = 5.0;
            if (bestSub >= 0 && best < hitTol)
            {
                targetSubpath = bestSub;
                insertIx = Math.Clamp(bestMark + 1, 0, _subpaths[targetSubpath].Knots.Count);
                makeSmooth = true;
                _activeSubpath = targetSubpath;
            }
        }
        var newKnot = new SplinePath.CP(new Vec2(p.X, p.Y), makeSmooth ? "smooth" : "corner", null, null);
        if (insertIx >= 0 && insertIx <= _subpaths[targetSubpath].Knots.Count) _subpaths[targetSubpath].Knots.Insert(insertIx, newKnot);
        else _subpaths[targetSubpath].Knots.Add(newKnot);
        _selection.Clear();
        _selection.Add(newKnot);
        _activeKnot = newKnot;
        // Enter creating mode (like demo): click and drag toggles corner/smooth based on radius
        _creating = !makeSmooth;
        _initPt = p;
        _dragging = !makeSmooth; // demo drags only when corner creating
        RenderAll();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var pv = e.GetPosition(EditorCanvas);
        _pointerPos = pv;
        // coordinates are already in content space
        var p = pv;
        if (_tool == ToolMode.FreehandTrace)
        {
            if (_freehandActive)
            {
                _freehandPoints.Add(p);
                RenderAll();
            }
            return;
        }
        if (_dragTan && _tanKnot != null)
        {
            UpdateTan(_tanKnot, p, e.KeyModifiers);
            RenderAll();
            return;
        }
        if (_tool == ToolMode.GlyphEdit)
        {
            var gsel = _glyphs.FirstOrDefault(g => g.Selected);
            if (gsel != null && _dragging)
            {
                // Move glyph in content coords
                double dx = p.X - (_lastPointerContent?.X ?? p.X);
                double dy = p.Y - (_lastPointerContent?.Y ?? p.Y);
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    var snapped = RoundToGrid(new Point(gsel.Ox + dx, gsel.Oy + dy));
                    gsel.Ox = snapped.X;
                    gsel.Oy = snapped.Y;
                }
                else
                {
                    gsel.Ox += dx;
                    gsel.Oy += dy;
                }
                _lastPointerContent = p;
                RenderAll();
            }
            return;
        }
        if (_creating)
        {
            // Toggle between corner and smooth while creating based on radius from initial click
            double r = Math.Sqrt((p.X - _initPt.X) * (p.X - _initPt.X) + (p.Y - _initPt.Y) * (p.Y - _initPt.Y));
            foreach (var knot in _selection)
            {
                knot.Ty = r < TanR1 ? "corner" : "smooth";
            }
            RenderAll();
            return;
        }
        if (_dragging)
        {
            if (_selection.Count == 0) return;
            double dx = p.X - (_lastPointerContent?.X ?? p.X);
            double dy = p.Y - (_lastPointerContent?.Y ?? p.Y);
            foreach (var k in _selection)
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _selection.Count == 1)
                {
                    var snapped = RoundToGrid(new Point(p.X, p.Y));
                    k.Pt = new Vec2(snapped.X, snapped.Y);
                }
                else
                {
                    k.Pt = new Vec2(k.Pt.X + dx, k.Pt.Y + dy);
                }
            }
            _lastPointerContent = p;
            RenderAll();
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // panning handled by ZoomHost
        if (_tool == ToolMode.FreehandTrace)
        {
            if (_freehandActive && _freehandPoints.Count > 2)
            {
                // Process freehand polyline into spline knots
                var created = ProcessFreehandPolyline();
                if (created != null)
                {
                    // append as a new subpath
                    _subpaths.Add(new SubpathData { Closed = false });
                    _subpaths[^1].Knots.AddRange(created);
                    _activeSubpath = _subpaths.Count - 1;
                }
            }
            _freehandActive = false;
            _freehandPoints.Clear();
            RenderAll();
            return;
        }

        _dragging = false;
        _dragTan = false;
        _activeKnot = null;
        _creating = false;
    }

    // Wheel zoom handled by ZoomHost in XAML control

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            foreach (var sp in _subpaths)
            {
                for (int i = 0; i < sp.Knots.Count; i++)
                {
                    if (_selection.Contains(sp.Knots[i]))
                    {
                        sp.Knots.RemoveAt(i);
                        i--;
                    }
                }
                if (sp.Knots.Count < 3) sp.Closed = false;
            }
            _selection.Clear();
            // Clear all state variables that might reference deleted knots
            _activeKnot = null;
            _dragging = false;
            _dragTan = false;
            RenderAll();
            e.Handled = true;
        }
    }

    private void RenderAll()
    {
        // Clear layers
        GridCanvas.Children.Clear();
        GlyphCanvas.Children.Clear();
        EditorCanvas.Children.Clear();

        // Transform applied by ZoomHost

        if (_showGrid)
        {
            double w = Math.Max(GridCanvas.Bounds.Width, 1);
            double h = Math.Max(GridCanvas.Bounds.Height, 1);
            double g = 20;
            for (double x = 0; x < w; x += g)
            {
                var line = new Line { StartPoint = new Point(x, 0), EndPoint = new Point(x, h), Stroke = new SolidColorBrush(Color.FromRgb(221,221,255)), StrokeThickness = 1, IsHitTestVisible = false };
                Canvas.SetLeft(line, 0);
                Canvas.SetTop(line, 0);
                GridCanvas.Children.Add(line);
            }
            for (double y = 0; y < h; y += g)
            {
                var line = new Line { StartPoint = new Point(0, y), EndPoint = new Point(w, y), Stroke = new SolidColorBrush(Color.FromRgb(221,221,255)), StrokeThickness = 1, IsHitTestVisible = false };
                GridCanvas.Children.Add(line);
            }
        }

        _paths.Clear();
        for (int si = 0; si < _subpaths.Count; si++)
        {
            var nks = _subpaths[si].Knots;
            if (nks.Count == 0) continue;
            var cps = new List<SplinePath.CP>(nks);
            var spline = new SplinePath(cps, _subpaths[si].Closed);
            spline.Solve();
            spline.ComputeCurvatureBlending();
            var bz = spline.Render(_useRawCubic);
            _paths.Add(bz);
            var pathStr = bz.ToSvgPath();
            if (!string.IsNullOrWhiteSpace(pathStr))
            {
                var path = new Avalonia.Controls.Shapes.Path
                {
                    Data = Geometry.Parse(pathStr),
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Fill = null,
                    IsHitTestVisible = false
                };
                EditorCanvas.Children.Add(path);
            }
        }

        // Draw freehand overlay when active
        if (_tool == ToolMode.FreehandTrace && _freehandPoints.Count > 1)
        {
            var poly = new Polyline
            {
                Points = new Avalonia.Collections.AvaloniaList<Point>(_freehandPoints),
                Stroke = Brushes.Yellow,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            EditorCanvas.Children.Add(poly);
        }

        RenderSelection();

        // Draw glyph overlays directly on canvas with explicit scale/translate (editable)
        if (_glyphs.Count > 0)
        {
            double cw = GlyphCanvas.Bounds.Width;
            double ch = GlyphCanvas.Bounds.Height;
            for (int gi = 0; gi < _glyphs.Count; gi++)
            {
                var g = _glyphs[gi];
                var gb = g.Geom.Bounds;
                if (g.NeedsAutoFit && cw >= 2 && ch >= 2 && gb.Width > 0 && gb.Height > 0)
                {
                    double sx = cw / gb.Width;
                    double sy = ch / gb.Height;
                    double s = Math.Min(sx, sy);
                    if (double.IsInfinity(s) || double.IsNaN(s) || s <= 0) s = 1.0;
                    double ox = (cw - s * gb.Width) * 0.5 - s * gb.X + 0.5 * s * gb.Width;
                    double oy = (ch - s * gb.Height) * 0.5 - s * gb.Y + 0.5 * s * gb.Height;
                    g.Scale = s; g.Ox = ox; g.Oy = oy; g.NeedsAutoFit = false;
                }
                var path = new Avalonia.Controls.Shapes.Path
                {
                    Data = g.Geom,
                    Stroke = g.Selected ? Brushes.DeepSkyBlue : Brushes.LightSlateGray,
                    StrokeThickness = g.Selected ? 1.5 : 1.0,
                    Fill = null,
                    IsHitTestVisible = false,
                    Opacity = 0.6,
                    RenderTransform = new MatrixTransform(new Matrix(g.Scale, 0, 0, g.Scale, g.Ox, g.Oy))
                };
                GlyphCanvas.Children.Add(path);
                if (g.Selected)
                {
                    // Draw bounds overlay using the same transform as the glyph for exact alignment
                    var rectGeo = new RectangleGeometry(new Rect(gb.X, gb.Y, gb.Width, gb.Height));
                    var rectPath = new Avalonia.Controls.Shapes.Path
                    {
                        Data = rectGeo,
                        Stroke = Brushes.DeepSkyBlue,
                        StrokeThickness = 1,
                        StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 4 },
                        Fill = null,
                        IsHitTestVisible = false,
                        RenderTransform = new MatrixTransform(new Matrix(g.Scale, 0, 0, g.Scale, g.Ox, g.Oy))
                    };
                    GlyphCanvas.Children.Add(rectPath);
                }
            }
        }

        // Hover-to-add indicator near curve (but not when knots are selected)
        if (EditorCanvas.IsPointerOver && _svgHover && _paths.Count > 0 && _selection.Count == 0)
        {
            var pos = this.GetPositionInCanvas();
            if (pos.HasValue)
            {
                var local = pos.Value;
                double best = double.MaxValue;
                foreach (var bz in _paths)
                {
                    var ht = bz.HitTest(local.X, local.Y);
                    if (ht.BestDist < best) best = ht.BestDist;
                }
                if (best < 5)
                {
                    var circ = new Ellipse { Width = 6, Height = 6, Stroke = Brushes.Black, StrokeThickness = 1, Fill = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)), IsHitTestVisible = false };
                    Canvas.SetLeft(circ, local.X - 3);
                    Canvas.SetTop(circ, local.Y - 3);
                    EditorCanvas.Children.Add(circ);
                }
            }
        }
    }

    private List<SplinePath.CP>? ProcessFreehandPolyline()
    {
        // Parameters can later be exposed via UI
        double simplifyTolerance = 2.0; // pixels
        double cornerAngleThresholdDeg = 35.0; // degrees
        double targetSpacing = 20.0; // pixels between knots in smooth spans
        double fitTolerance = 2.5; // optional refinement tolerance (pixels)
        int maxIterations = 2; // lightweight refinement

        // 1) Simplify with RDP
        var freehand = _freehandPoints.Select(p => new Vec2(p.X, p.Y)).ToList();
        var simplified = PolylineUtils.RamerDouglasPeucker(freehand, simplifyTolerance);
        if (simplified.Count < 2) return null;

        // 2) Detect corners
        var corners = PolylineUtils.DetectCorners(simplified, cornerAngleThresholdDeg * Math.PI / 180.0);

        // 3) Segment at corners and place knots
        var newKnots = new List<SplinePath.CP>();
        int start = 0;
        for (int i = 0; i < simplified.Count; i++)
        {
            bool isCornerHere = corners.Contains(i) || i == simplified.Count - 1;
            if (isCornerHere)
            {
                // Resample the span [start, i] by arc-length spacing
                var sampled = PolylineUtils.ResampleBySpacing(simplified, start, i, targetSpacing);
                for (int s = 0; s < sampled.Count; s++)
                {
                    var pt = sampled[s];
                    bool isCorner = (s == 0 && newKnots.Count == 0) || (s == sampled.Count - 1) || corners.Contains(start + s);
                    var cp = new SplinePath.CP(new Vec2(pt.X, pt.Y), isCorner ? "corner" : "smooth", null, null);
                    // De-duplicate close points
                    if (newKnots.Count == 0 || Math.Abs(newKnots[^1].Pt.X - cp.Pt.X) + Math.Abs(newKnots[^1].Pt.Y - cp.Pt.Y) > 0.5)
                    {
                        newKnots.Add(cp);
                    }
                }
                start = i;
            }
        }

        // Clamp to at least two knots
        if (newKnots.Count < 2) return null;

        // 4) Optional refinement by max deviation (coarse):
        //    evaluate current spline and insert extra knots at max-error locations
        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Assign and solve current knots
            // Use a local spline for refinement (do not mutate current state)
            var tempKnots = new List<SplinePath.CP>(newKnots.Select(k => new SplinePath.CP(new Vec2(k.Pt.X, k.Pt.Y), k.Ty, k.LTh, k.RTh)));

            var spline = new SplinePath(tempKnots, false);
            spline.Solve();
            spline.ComputeCurvatureBlending();
            var path = spline.Render(_useRawCubic);

            // Sample the simplified polyline and find worst deviation to path
            double maxErr = 0;
            Vec2? worstPt = null;
            for (int i = 0; i < simplified.Count; i++)
            {
                var p = simplified[i];
                var ht = path.HitTest(p.X, p.Y);
                if (ht.BestDist > maxErr)
                {
                    maxErr = ht.BestDist;
                    worstPt = p;
                }
            }
            if (worstPt == null || maxErr <= fitTolerance) break;

            // Insert a knot at the worst point's nearest segment location
            int insertIx = newKnots.Count;
            var ht2 = path.HitTest(worstPt.Value.X, worstPt.Value.Y);
            if (ht2.BestMark.HasValue) insertIx = Math.Clamp(ht2.BestMark.Value + 1, 1, newKnots.Count);
            newKnots.Insert(insertIx, new SplinePath.CP(new Vec2(worstPt.Value.X, worstPt.Value.Y), "smooth", null, null));
        }
        return newKnots;
    }

    private Point? GetPositionInCanvas()
    {
        if (_pointerPos.HasValue)
        {
            // _pointerPos is in view space; check against view bounds, then convert to content
            var viewPt = _pointerPos.Value;
            if (viewPt.X >= 0 && viewPt.Y >= 0 && viewPt.X <= Viewport.Bounds.Width && viewPt.Y <= Viewport.Bounds.Height)
                return ViewToContent(viewPt);
        }
        return null;
    }

    private sealed class TanHandleTag
    {
        public required SplinePath.CP Knot { get; init; }
        public required bool IsRight { get; init; }
    }

    private void RenderSelection()
    {
        foreach (var sp in _subpaths)
        {
            foreach (var k in sp.Knots)
            {
                bool isSelected = _selection.Contains(k);
            Shape handleShape;
            if (k.Ty == "corner")
            {
                handleShape = new Rectangle
                {
                    Width = 8,
                    Height = 8
                };
                Canvas.SetLeft(handleShape, k.Pt.X - 4);
                Canvas.SetTop(handleShape, k.Pt.Y - 4);
            }
            else
            {
                handleShape = new Ellipse
                {
                    Width = 8,
                    Height = 8
                };
                Canvas.SetLeft(handleShape, k.Pt.X - 4);
                Canvas.SetTop(handleShape, k.Pt.Y - 4);
            }
            // Default visual behavior mirrors CSS:
            // - When canvas hovered: all handles filled dark red (#800); selected filled #fcb with black stroke
            // - When not hovered: handles transparent
            if (_svgHover)
            {
                if (isSelected)
                {
                    handleShape.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xBB));
                    handleShape.Stroke = Brushes.Black;
                    handleShape.StrokeThickness = 1;
                }
                else
                {
                    handleShape.Fill = new SolidColorBrush(Color.FromRgb(0x88, 0x00, 0x00));
                    handleShape.Stroke = null;
                }
                handleShape.PointerEntered += (_, __) => handleShape.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x00));
                handleShape.PointerExited += (_, __) =>
                {
                    if (isSelected)
                    {
                        handleShape.Fill = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0xBB));
                        handleShape.Stroke = Brushes.Black;
                        handleShape.StrokeThickness = 1;
                    }
                    else
                    {
                        handleShape.Fill = new SolidColorBrush(Color.FromRgb(0x88, 0x00, 0x00));
                        handleShape.Stroke = null;
                    }
                };
            }
            else
            {
                handleShape.Fill = Brushes.Transparent;
                handleShape.Stroke = null;
                handleShape.PointerEntered += (_, __) => handleShape.Fill = Brushes.Transparent;
                handleShape.PointerExited += (_, __) => handleShape.Fill = Brushes.Transparent;
            }
            handleShape.IsHitTestVisible = true;
            EditorCanvas.Children.Add(handleShape);

            bool drawCirc = isSelected && !_dragging;
            bool lComputed = k.LTh == null;
            bool rComputed = k.RTh == null;
            // Determine angles to draw using computed when not explicit
            double? lth = (!lComputed || drawCirc) ? (k.LTh ?? (drawCirc ? k.LThComputed : (double?)null)) : null;
            double? rth = (!rComputed || drawCirc) ? (k.RTh ?? (drawCirc ? k.RThComputed : (double?)null)) : null;
            // Lines
            if (rth != null)
            {
                var line = new Line
                {
                    StartPoint = new Point(k.Pt.X, k.Pt.Y),
                    EndPoint = new Point(k.Pt.X + (TanR2 - 3) * Math.Cos(rth.Value), k.Pt.Y + (TanR2 - 3) * Math.Sin(rth.Value)),
                    Stroke = rComputed ? new SolidColorBrush(Color.FromArgb(80, 0, 0, 255)) : Brushes.Blue,
                    StrokeThickness = 1,
                    IsHitTestVisible = false
                };
                EditorCanvas.Children.Add(line);
            }
            if (lth != null)
            {
                var line = new Line
                {
                    StartPoint = new Point(k.Pt.X, k.Pt.Y),
                    EndPoint = new Point(k.Pt.X + (-TanR2 + 3) * Math.Cos(lth.Value), k.Pt.Y + (-TanR2 + 3) * Math.Sin(lth.Value)),
                    Stroke = lComputed ? new SolidColorBrush(Color.FromArgb(80, 0, 0, 255)) : Brushes.Blue,
                    StrokeThickness = 1,
                    IsHitTestVisible = false
                };
                EditorCanvas.Children.Add(line);
            }
            // Tangent handle circles (interactive)
            if (drawCirc)
            {
                if (lth != null)
                {
                    var circ = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = lComputed ? new SolidColorBrush(Color.FromArgb(77, 255, 255, 255)) : Brushes.White,
                        Stroke = lComputed ? new SolidColorBrush(Color.FromArgb(77, 0, 0, 255)) : Brushes.Blue,
                        StrokeThickness = 1,
                        Tag = new TanHandleTag { Knot = k, IsRight = false }
                    };
                    Canvas.SetLeft(circ, k.Pt.X + (-TanR2) * Math.Cos(lth.Value) - 3);
                    Canvas.SetTop(circ, k.Pt.Y + (-TanR2) * Math.Sin(lth.Value) - 3);
                    circ.PointerPressed += OnPointerPressed; // reuse handler to initiate drag
                    EditorCanvas.Children.Add(circ);
                }
                if (rth != null)
                {
                    var circ = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = rComputed ? new SolidColorBrush(Color.FromArgb(77, 255, 255, 255)) : Brushes.White,
                        Stroke = rComputed ? new SolidColorBrush(Color.FromArgb(77, 0, 0, 255)) : Brushes.Blue,
                        StrokeThickness = 1,
                        Tag = new TanHandleTag { Knot = k, IsRight = true }
                    };
                    Canvas.SetLeft(circ, k.Pt.X + (TanR2) * Math.Cos(rth.Value) - 3);
                    Canvas.SetTop(circ, k.Pt.Y + (TanR2) * Math.Sin(rth.Value) - 3);
                    circ.PointerPressed += OnPointerPressed;
                    EditorCanvas.Children.Add(circ);
                }
            }
            }
        }
    }

    private void UpdateTan(SplinePath.CP knot, Point pt, KeyModifiers mods)
    {
        double dx = pt.X - _tanInitPt.X;
        double dy = pt.Y - _tanInitPt.Y;
        if (!_tanIsRight)
        {
            dx = -dx;
            dy = -dy;
        }
        double r = Math.Sqrt(dx * dx + dy * dy);
        double? th = null;
        if (r < TanR3)
        {
            th = Math.Atan2(dy, dx);
            if (mods.HasFlag(KeyModifiers.Shift))
            {
                th = Math.PI / 2 * Math.Round(th.Value * (2 / Math.PI));
            }
        }
        if (knot.Ty == "smooth" || !_tanIsRight)
            knot.LTh = th;
        if (knot.Ty == "smooth" || _tanIsRight)
            knot.RTh = th;
    }

    private Point RoundToGrid(Point p, double g = 20)
    {
        return new Point(g * Math.Round(p.X / g), g * Math.Round(p.Y / g));
    }

    // ===== View transform helpers =====
    private void ApplyViewTransform()
    {
        // Transform is handled by ZoomHost control
    }

    private Point ViewToContent(Point viewPoint)
    {
        // ZoomBorder does not expose conversion helpers; our content is drawn in content space
        return viewPoint;
    }

    private Point ContentToView(Point contentPoint)
    {
        // Same as above
        return contentPoint;
    }

    private void ZoomAroundContentPoint(Point contentAnchor, double factor)
    {
        // Not supported directly; wheel zoom handles user interaction
        RenderAll();
    }

    private void ZoomAroundViewPoint(Point viewAnchor, double factor)
    {
        // Not supported directly; wheel zoom handles user interaction
        RenderAll();
    }

    // Zoom to a view point and center that content point in the viewport after zoom
    private void ZoomToViewPointAndCenter(Point viewAnchor, double factor)
    {
        // Optional: could implement centering via ZoomHost; keeping old API for menu usage
        ZoomAroundViewPoint(viewAnchor, factor);
    }

    private void SanitizeTransform()
    {
        // No-op: transform validity enforced by ZoomHost
    }

    private void FitContentToView()
    {
        double vw = Math.Max(EditorCanvas.Bounds.Width, 1);
        double vh = Math.Max(EditorCanvas.Bounds.Height, 1);
        if (vw <= 1 || vh <= 1)
        {
            ZoomHost.ResetMatrix();
            RenderAll();
            return;
        }

        bool hasAny = false;
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;

        // Include knots (content coordinates)
        foreach (var sp in _subpaths)
        {
            foreach (var k in sp.Knots)
            {
                hasAny = true;
                if (k.Pt.X < minX) minX = k.Pt.X;
                if (k.Pt.Y < minY) minY = k.Pt.Y;
                if (k.Pt.X > maxX) maxX = k.Pt.X;
                if (k.Pt.Y > maxY) maxY = k.Pt.Y;
            }
        }
        // Include glyphs (their geometry bounds with scale/offset)
        foreach (var g in _glyphs)
        {
            var b = g.Geom.Bounds;
            // transform rectangle corners by [s 0 0 s Ox Oy]
            var c0 = new Point(g.Ox + g.Scale * b.X, g.Oy + g.Scale * b.Y);
            var c1 = new Point(g.Ox + g.Scale * (b.X + b.Width), g.Oy + g.Scale * b.Y);
            var c2 = new Point(g.Ox + g.Scale * (b.X + b.Width), g.Oy + g.Scale * (b.Y + b.Height));
            var c3 = new Point(g.Ox + g.Scale * b.X, g.Oy + g.Scale * (b.Y + b.Height));
            hasAny = true;
            double gxmin = new[] { c0.X, c1.X, c2.X, c3.X }.Min();
            double gxmax = new[] { c0.X, c1.X, c2.X, c3.X }.Max();
            double gymin = new[] { c0.Y, c1.Y, c2.Y, c3.Y }.Min();
            double gymax = new[] { c0.Y, c1.Y, c2.Y, c3.Y }.Max();
            if (gxmin < minX) minX = gxmin;
            if (gymin < minY) minY = gymin;
            if (gxmax > maxX) maxX = gxmax;
            if (gymax > maxY) maxY = gymax;
        }

        if (!hasAny || !double.IsFinite(minX) || !double.IsFinite(minY) || !double.IsFinite(maxX) || !double.IsFinite(maxY))
        {
            ZoomHost.ResetMatrix();
            RenderAll();
            return;
        }

        // Add small padding then call AutoFit on ZoomBorder
        double pad = 20.0;
        minX -= pad; minY -= pad; maxX += pad; maxY += pad;
        ZoomHost.AutoFit();
        // ZoomBorder manages centering within AutoFit/Stretch modes
        RenderAll();
    }

    private SubpathData EnsureActiveSubpath()
    {
        if (_subpaths.Count == 0)
        {
            _subpaths.Add(new SubpathData { Closed = false });
            _activeSubpath = 0;
        }
        if (_activeSubpath < 0 || _activeSubpath >= _subpaths.Count) _activeSubpath = 0;
        return _subpaths[_activeSubpath];
    }

    private async Task MessageBox(string message)
    {
        var dlg = new Window
        {
            Width = 400,
            Height = 140,
            Title = "Info",
            Content = new StackPanel
            {
                Margin = new Thickness(10),
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Margin = new Thickness(0,10,0,0) }
                }
            }
        };
        if (dlg.Content is StackPanel sp && sp.Children.OfType<Button>().FirstOrDefault() is Button ok)
        {
            ok.Click += (_, __) => dlg.Close();
        }
        await dlg.ShowDialog(this);
    }

    private string Serialize()
    {
        static double R(double x, double adj) => Math.Round(x * adj) / adj;
        var subpaths = new List<object?>();
        for (int si = 0; si < _subpaths.Count; si++)
        {
            var pts = new List<Dictionary<string, object?>>();
            foreach (var k in _subpaths[si].Knots)
            {
                var pt = new Dictionary<string, object?>
                {
                    ["x"] = R(k.Pt.X, 100),
                    ["y"] = R(k.Pt.Y, 100)
                };
                // Match demo: c=1 means smooth; c=0 means corner
                pt["c"] = k.Ty == "smooth" ? 1 : 0;
                if (k.Ty == "smooth")
                {
                    if (k.LTh != null) pt["t"] = R(k.LTh.Value, 1000);
                }
                else
                {
                    if (k.LTh != null) pt["l"] = R(k.LTh.Value, 1000);
                    if (k.RTh != null) pt["r"] = R(k.RTh.Value, 1000);
                }
                pts.Add(pt);
            }
            var sp = new Dictionary<string, object?>
            {
                ["closed"] = _subpaths[si].Closed,
                ["pts"] = pts
            };
            subpaths.Add(sp);
        }
        var result = new Dictionary<string, object?>
        {
            ["subpaths"] = subpaths
        };
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    private void Deserialize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        _subpaths.Clear();
        var sps = root.GetProperty("subpaths");
        foreach (var sp in sps.EnumerateArray())
        {
            bool closed = sp.GetProperty("closed").GetBoolean();
            var knots = new List<SplinePath.CP>();
            foreach (var pt in sp.GetProperty("pts").EnumerateArray())
            {
                string ty = pt.GetProperty("c").GetInt32() != 0 ? "smooth" : "corner";
                double x = pt.GetProperty("x").GetDouble();
                double y = pt.GetProperty("y").GetDouble();
                var knot = new SplinePath.CP(new Vec2(x, y), ty, null, null);
                if (ty == "smooth")
                {
                    if (pt.TryGetProperty("t", out var tprop))
                    {
                        knot.LTh = tprop.GetDouble();
                        knot.RTh = knot.LTh;
                    }
                }
                else
                {
                    if (pt.TryGetProperty("l", out var lprop)) knot.LTh = lprop.GetDouble();
                    if (pt.TryGetProperty("r", out var rprop)) knot.RTh = rprop.GetDouble();
                }
                knots.Add(knot);
            }
            _subpaths.Add(new SubpathData { Closed = closed });
            _subpaths[^1].Knots.AddRange(knots);
        }
        _activeSubpath = _subpaths.Count > 0 ? 0 : -1;
        _selection.Clear();
        RenderAll();
    }
}
