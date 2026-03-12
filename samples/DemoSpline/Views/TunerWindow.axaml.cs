using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Spline;

namespace DemoSpline.Views;

public partial class TunerWindow : Window
{
    private int _n = 6;
    private double _scale = 0.9;
    private int _mapSize = 480;

    private double _bx0 = 500;
    private double _by0 = 100;
    private double _bdx = 280;

    private double _cx0;
    private double _cy0 = 300;
    private double _cdx = 200;

    private CurveGrid _grid = null!;
    private readonly List<Ellipse> _ctrlPts = new();
    private CtrlPt? _dragObj;
    private int _gridChildCount;

    private sealed class CtrlPt
    {
        public readonly TunerWindow T;
        public readonly int I;
        public readonly int J;
        public readonly int K; // 0..3
        public CtrlPt(TunerWindow t, int i, int j, int k) { T = t; I = i; J = j; K = k; }
        public TwoCubics GetMaster() => T._grid.GetMaster(I, J);
        public Vec2 GetPt(double th0, double th1)
        {
            var r = GetMaster().Render(th0, th1);
            int rix = K < 2 ? K : K + 1;
            return r[rix];
        }
    }

    public TunerWindow()
    {
        InitializeComponent();
        _cx0 = _bx0;
        TunerCanvas.PointerPressed += OnPointerDown;
        TunerCanvas.PointerMoved += OnPointerMove;
        TunerCanvas.PointerReleased += OnPointerUp;
        SetUpGrid();
        RedrawInterp(Math.PI * 0.25, 0);
    }

    private void SetUpGrid()
    {
        var masters = new List<TwoCubics>();
        for (int i = 0; i <= _n; i++)
        {
            double th0 = 0.5 * Math.PI * i / _n;
            for (int j = -i; j <= i; j++)
            {
                double th1 = 0.5 * Math.PI * j / _n;
                var cubic = new CubicBezier(TwoParamCurve_MyCubic(th0, th1));
                masters.Add(TwoCubics.Raise(cubic));
            }
        }
        _grid = new CurveGrid(_n, masters);

        // draw lattice points (like original demo – small black dots)
        for (int i = 0; i <= _n; i++)
        {
            for (int j = -i; j <= i; j++)
            {
                var pt = GridToXy(i, j);
                var circ = new Ellipse { Width = 2, Height = 2, Fill = Brushes.Black, IsHitTestVisible = false };
                Canvas.SetLeft(circ, pt.X - 1);
                Canvas.SetTop(circ, pt.Y - 1);
                TunerCanvas.Children.Add(circ);
            }
        }
        _gridChildCount = TunerCanvas.Children.Count;
    }

    private static double[] TwoParamCurve_MyCubic(double th0, double th1)
    {
        double MyCubicLen(double a0, double a1)
        {
            double offset = 0.3 * Math.Sin(a1 * 2 - 0.4 * Math.Sin(a1 * 2));
            double scale = 1.0 / (3 * 0.8);
            double len = scale * (Math.Cos(a0 - offset) - 0.2 * Math.Cos(3 * (a0 - offset)));
            return len;
        }
        var coords = new double[8];
        double len0 = MyCubicLen(th0, th1);
        coords[2] = Math.Cos(th0) * len0;
        coords[3] = Math.Sin(th0) * len0;
        double len1 = MyCubicLen(th1, th0);
        coords[4] = 1 - Math.Cos(th1) * len1;
        coords[5] = Math.Sin(th1) * len1;
        coords[6] = 1;
        return coords;
    }

    private Point GridToXy(int i, int j)
    {
        double x = _mapSize * 0.5 * (1 + _scale / _n * i);
        double y = _mapSize * 0.5 * (1 - _scale / _n * j);
        return new Point(x, y);
    }

    private void RenderCurve(TwoCubics two, double th0, double th1)
    {
        // Clear prior drawings except background grid/lattice
        while (TunerCanvas.Children.Count > _gridChildCount)
        {
            var lastChild = TunerCanvas.Children[TunerCanvas.Children.Count - 1];
            TunerCanvas.Children.Remove(lastChild);
        }

        var r = two.Render(th0, th1);
        // Curve path (SVG) using invariant culture
        var sb = new System.Text.StringBuilder();
        sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "M{0} {1}", _bx0, _by0);
        string cmd = " C";
        for (int j = 0; j < r.Count; j++)
        {
            var pt = r[j];
            double x = _bx0 + _bdx * pt.X;
            double y = _by0 - _bdx * pt.Y;
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}{1} {2}", cmd, x, y);
            cmd = " ";
        }
        sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, " {0} {1}", _bx0 + _bdx, _by0);
        string path = sb.ToString();
        var geomPath = new Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse(path),
            Stroke = Brushes.Blue,
            StrokeThickness = 1,
            Fill = null,
            IsHitTestVisible = false
        };
        TunerCanvas.Children.Add(geomPath);

        var center = two.GetCenterPt(th0, th1);
        var ctr = new Ellipse { Width = 4, Height = 4, Fill = Brushes.Green };
        Canvas.SetLeft(ctr, _bx0 + _bdx * center.X - 2);
        Canvas.SetTop(ctr, _by0 - _bdx * center.Y - 2);
        TunerCanvas.Children.Add(ctr);

        // Curvature plot
        var curvSb = new System.Text.StringBuilder();
        string ccmd = "M";
        var last = new Vec2(0, 0);
        double s = 0;
        int n = 100;
        CubicBezier cb = null!;
        for (int i = 0; i <= n; i++)
        {
            if (i == 0 || i == n / 2)
            {
                var coords = new double[8];
                for (int j = 0; j < 4; j++)
                {
                    int ix = i == 0 ? j - 1 : j + 2;
                    var pt = ix < 0 ? new Vec2(0, 0) : ix > 4 ? new Vec2(1, 0) : r[ix];
                    coords[j * 2] = pt.X;
                    coords[j * 2 + 1] = pt.Y;
                }
                cb = new CubicBezier(coords);
            }
            double t = (i < n / 2 ? i : i - n / 2) * (2.0 / n);
            var xy = cb.Eval(t);
            s += MathUtils.Hypot(xy.X - last.X, xy.Y - last.Y);
            last = xy;
            double x2 = _cx0 + _cdx * s;
            double k = cb.Curvature(t);
            if (!double.IsNaN(k))
            {
                double scale = -0.05;
                double y2 = _cy0 - _cdx * scale * k;
                curvSb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}{1} {2}", ccmd, x2, y2);
                ccmd = " L";
            }
        }
        string curvPath = curvSb.ToString();
        var curvGeom = new Avalonia.Controls.Shapes.Path
        {
            Data = Geometry.Parse(curvPath),
            Stroke = Brushes.Blue,
            StrokeThickness = 1,
            Fill = null,
            IsHitTestVisible = false
        };
        TunerCanvas.Children.Add(curvGeom);
        var baseLine = new Line { StartPoint = new Point(_cx0, _cy0), EndPoint = new Point(_cx0 + _cdx * s, _cy0), Stroke = Brushes.Black, IsHitTestVisible = false };
        TunerCanvas.Children.Add(baseLine);
    }

    private void RedrawInterp(double th0, double th1)
    {
        var interp = _grid.GetInterp(th0, th1);
        RenderCurve(interp, th0, th1);
    }

    private void OnPointerDown(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var p = e.GetPosition(TunerCanvas);
        // if clicking empty space in map area, show interpolated curve immediately (hover behavior)
        double th0h = (2.0 / _mapSize * p.X - 1) / _scale * 0.5 * Math.PI;
        double th1h = (1 - 2.0 / _mapSize * p.Y) / _scale * 0.5 * Math.PI;
        RedrawInterp(th0h, th1h);
        // ctrl points drag test (only if pointer is close to the visible 6px control markers)
        foreach (var cpt in _ctrlPts)
        {
            double cx = Canvas.GetLeft(cpt) + 3;
            double cy = Canvas.GetTop(cpt) + 3;
            if (Math.Abs(cx - p.X) < 6 && Math.Abs(cy - p.Y) < 6)
            {
                _dragObj = (CtrlPt?)cpt.Tag;
                return;
            }
        }

        int i = (int)Math.Round((2.0 / _mapSize * p.X - 1) * _n / _scale);
        int j = (int)Math.Round((1 - 2.0 / _mapSize * p.Y) * _n / _scale);
        var quant = GridToXy(i, j);
        double hitDist = MathUtils.Hypot(quant.X - p.X, quant.Y - p.Y);
        if (hitDist < 6 && i >= 0 && j >= -i && j <= i)
        {
            var master = _grid.GetMaster(i, j);
            double th0 = i * 0.5 * Math.PI / _n;
            double th1 = j * 0.5 * Math.PI / _n;
            RenderCurve(master, th0, th1);
            foreach (var c in _ctrlPts) TunerCanvas.Children.Remove(c);
            _ctrlPts.Clear();
            for (int k = 0; k < 4; k++)
            {
                var cpt = new CtrlPt(this, i, j, k);
                var pt = cpt.GetPt(th0, th1);
                double x = _bx0 + _bdx * pt.X;
                double y = _by0 - _bdx * pt.Y;
                var circ = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Black, Tag = cpt };
                Canvas.SetLeft(circ, x - 3);
                Canvas.SetTop(circ, y - 3);
                TunerCanvas.Children.Add(circ);
                _ctrlPts.Add(circ);
            }
        }
        // otherwise we leave the interpolated curve (top area) responding to move
    }

    private void OnPointerMove(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var p = e.GetPosition(TunerCanvas);
        if (_dragObj != null)
        {
            double x = (p.X - _bx0) / _bdx;
            double y = (_by0 - p.Y) / _bdx;
            var master = _dragObj.GetMaster();
            bool sym = _dragObj.I == _dragObj.J;
            bool antisym = _dragObj.I == -_dragObj.J;
            if (_dragObj.K == 0)
            {
                master.A[0] = MathUtils.Hypot(x, y);
                if (sym || antisym) master.A[5] = master.A[0];
            }
            else if (_dragObj.K == 1)
            {
                master.A[1] = x; master.A[2] = y;
                if (sym) { master.A[3] = 1 - x; master.A[4] = y; }
                else if (antisym) { master.A[3] = 1 - x; master.A[4] = -y; }
            }
            else if (_dragObj.K == 2)
            {
                master.A[3] = x; master.A[4] = y;
                if (sym) { master.A[1] = 1 - x; master.A[2] = y; }
                else if (antisym) { master.A[1] = 1 - x; master.A[2] = -y; }
            }
            else if (_dragObj.K == 3)
            {
                master.A[5] = MathUtils.Hypot(1 - x, y);
                if (sym || antisym) master.A[0] = master.A[5];
            }
            double th0 = _dragObj.I * 0.5 * Math.PI / _n;
            double th1 = _dragObj.J * 0.5 * Math.PI / _n;
            RenderCurve(master, th0, th1);
            // Update control points visualization
            for (int idx = 0; idx < _ctrlPts.Count; idx++)
            {
                var cpt = (CtrlPt)_ctrlPts[idx].Tag!;
                var pt2 = cpt.GetPt(th0, th1);
                double x2 = _bx0 + _bdx * pt2.X;
                double y2 = _by0 - _bdx * pt2.Y;
                Canvas.SetLeft(_ctrlPts[idx], x2 - 3);
                Canvas.SetTop(_ctrlPts[idx], y2 - 3);
            }
        }
        else
        {
            double th0 = (2.0 / _mapSize * p.X - 1) / _scale * 0.5 * Math.PI;
            double th1 = (1 - 2.0 / _mapSize * p.Y) / _scale * 0.5 * Math.PI;
            RedrawInterp(th0, th1);
        }
    }

    private void OnPointerUp(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _dragObj = null;
    }

    private async void OnLoad(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (this.StorageProvider is null) return;
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Open Tuner Grid",
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
            _grid = CurveGrid.FromJson(json);
            JsonBox.Text = json;
        }
        catch (Exception ex)
        {
            await MessageBox($"Failed to load: {ex.Message}");
        }
    }

    private async void OnSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (this.StorageProvider is null) return;
        try
        {
            string json = _grid.ToJson();
            JsonBox.Text = json;
            var result = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Tuner Grid",
                SuggestedFileName = "grid.json",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("JSON") { Patterns = new List<string> { "*.json" } }
                }
            });
            if (result is null) return;
            await using var s = await result.OpenWriteAsync();
            using var w = new StreamWriter(s);
            await w.WriteAsync(json);
        }
        catch (Exception ex)
        {
            await MessageBox($"Failed to save: {ex.Message}");
        }
    }

    private void OnMap(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        int W = _mapSize, H = _mapSize;
        var wb = new WriteableBitmap(new PixelSize(W, H), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);
        using (var fb = wb.Lock())
        {
            var span = new byte[fb.RowBytes * H];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    double th0 = (2.0 / _mapSize * x - 1) / _scale * 0.5 * Math.PI;
                    double th1 = (1 - 2.0 / _mapSize * y) / _scale * 0.5 * Math.PI;
                    int ix = y * fb.RowBytes + x * 4;
                    var interp = _grid.GetInterp(th0, th1);
                    double atanK = interp.AtanCurvature(th0, th1);
                    double k = Math.Tan(atanK);
                    double k1 = 0.25 * k;
                    double scaled = Math.Asin((Math.Sqrt(4 * k1 * k1 + 1) - 1) / (2 * k1));
                    if (atanK > Math.PI / 2) scaled += Math.PI;
                    else if (atanK < -Math.PI / 2) scaled -= Math.PI;
                    double z = 0.3 * scaled;
                    double rr = 128 - 127 * z;
                    double gg = 112 + 2 * (((int)rr) & 8);
                    double bb = 128 + 127 * z;
                    byte r = (byte)Math.Clamp((int)Math.Round(rr), 0, 255);
                    byte g = (byte)Math.Clamp((int)Math.Round(gg), 0, 255);
                    byte b = (byte)Math.Clamp((int)Math.Round(bb), 0, 255);
                    span[ix + 0] = b;
                    span[ix + 1] = g;
                    span[ix + 2] = r;
                    span[ix + 3] = 255;
                }
            }
            System.Runtime.InteropServices.Marshal.Copy(span, 0, fb.Address, span.Length);
        }
        MapImage.Source = wb;
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
}
