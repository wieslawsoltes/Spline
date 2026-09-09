using System;
using System.Buffers;
using System.Collections.Generic;

namespace Spline;

/// <summary>
/// Represents a non-rational B-spline curve using a degree, knot vector, and control polygon.
/// </summary>
public sealed class BSplineCurve
{
    private const int StackControlPointThreshold = 64;
    private const int StackDegreeThreshold = 16;

    private readonly Vec2[] _controlPoints;
    private readonly double[] _knots;
    private readonly double[] _weights;
    private readonly IReadOnlyList<Vec2> _controlPointsView;
    private readonly IReadOnlyList<double> _knotsView;
    private readonly IReadOnlyList<double> _weightsView;

    /// <summary>
    /// Initializes a non-rational B-spline curve.
    /// </summary>
    /// <param name="degree">Polynomial degree. Must be at least one.</param>
    /// <param name="controlPoints">B-spline control points.</param>
    /// <param name="knots">Nondecreasing knot vector.</param>
    public BSplineCurve(int degree, IReadOnlyList<Vec2> controlPoints, IReadOnlyList<double> knots)
    {
        ArgumentNullException.ThrowIfNull(controlPoints);
        ArgumentNullException.ThrowIfNull(knots);

        if (degree < 1)
            throw new ArgumentOutOfRangeException(nameof(degree), degree, "Degree must be at least one.");
        if (controlPoints.Count < degree + 1)
            throw new ArgumentException("The control-point count must be at least degree + 1.", nameof(controlPoints));
        if (knots.Count != controlPoints.Count + degree + 1)
            throw new ArgumentException("The knot count must equal controlPointCount + degree + 1.", nameof(knots));

        Degree = degree;
        _controlPoints = new Vec2[controlPoints.Count];
        _knots = new double[knots.Count];
        _weights = new double[controlPoints.Count];

        for (int i = 0; i < _controlPoints.Length; i++)
        {
            var point = controlPoints[i];
            if (!IsFinite(point.X) || !IsFinite(point.Y))
                throw new ArgumentException($"Control point {i} contains a non-finite coordinate.", nameof(controlPoints));

            _controlPoints[i] = point;
            _weights[i] = 1.0;
        }

        double previous = double.NegativeInfinity;
        for (int i = 0; i < _knots.Length; i++)
        {
            double knot = knots[i];
            if (!IsFinite(knot))
                throw new ArgumentException($"Knot {i} is not finite.", nameof(knots));
            if (knot < previous)
                throw new ArgumentException("Knots must be nondecreasing.", nameof(knots));

            _knots[i] = knot;
            previous = knot;
        }

        DomainStart = _knots[Degree];
        DomainEnd = _knots[_controlPoints.Length];
        if (!(DomainEnd > DomainStart))
            throw new ArgumentException("The active B-spline parameter domain must have positive length.", nameof(knots));

        _controlPointsView = Array.AsReadOnly(_controlPoints);
        _knotsView = Array.AsReadOnly(_knots);
        _weightsView = Array.AsReadOnly(_weights);
    }

    /// <summary>
    /// Gets the polynomial degree.
    /// </summary>
    public int Degree { get; }

    /// <summary>
    /// Gets the B-spline control polygon.
    /// </summary>
    public IReadOnlyList<Vec2> ControlPoints => _controlPointsView;

    /// <summary>
    /// Gets the nondecreasing knot vector.
    /// </summary>
    public IReadOnlyList<double> Knots => _knotsView;

    /// <summary>
    /// Gets unit weights for CAD/NURBS interchange. This curve is non-rational.
    /// </summary>
    public IReadOnlyList<double> Weights => _weightsView;

    /// <summary>
    /// Gets the first active parameter in the knot vector.
    /// </summary>
    public double DomainStart { get; }

    /// <summary>
    /// Gets the last active parameter in the knot vector.
    /// </summary>
    public double DomainEnd { get; }

    /// <summary>
    /// Gets a value indicating that this curve is non-rational.
    /// </summary>
    public bool IsRational => false;

    /// <summary>
    /// Evaluates the curve at a parameter using de Boor's algorithm.
    /// </summary>
    public Vec2 Evaluate(double parameter)
    {
        ValidateParameter(parameter);
        return EvaluateCore(_controlPoints, Degree, _knots, parameter);
    }

    /// <summary>
    /// Evaluates a derivative of the curve. Order zero evaluates the curve itself.
    /// Orders above the curve degree are identically zero.
    /// </summary>
    public Vec2 EvaluateDerivative(double parameter, int order = 1)
    {
        if (order < 0)
            throw new ArgumentOutOfRangeException(nameof(order), order, "Derivative order cannot be negative.");

        ValidateParameter(parameter);
        if (order == 0)
            return EvaluateCore(_controlPoints, Degree, _knots, parameter);
        if (order > Degree)
            return default;

        Vec2[]? rented = null;
        Span<Vec2> work = _controlPoints.Length <= StackControlPointThreshold
            ? stackalloc Vec2[_controlPoints.Length]
            : (rented = ArrayPool<Vec2>.Shared.Rent(_controlPoints.Length)).AsSpan(0, _controlPoints.Length);

        try
        {
            _controlPoints.AsSpan().CopyTo(work);

            int currentCount = _controlPoints.Length;
            int currentDegree = Degree;
            int knotOffset = 0;

            for (int derivative = 0; derivative < order; derivative++)
            {
                for (int i = 0; i < currentCount - 1; i++)
                {
                    double denominator = _knots[knotOffset + i + currentDegree + 1]
                                       - _knots[knotOffset + i + 1];
                    work[i] = denominator == 0
                        ? default
                        : (work[i + 1] - work[i]) * (currentDegree / denominator);
                }

                currentCount--;
                currentDegree--;
                knotOffset++;
            }

            var derivativeKnots = _knots.AsSpan(knotOffset, _knots.Length - 2 * knotOffset);
            return EvaluateCore(work[..currentCount], currentDegree, derivativeKnots, parameter);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<Vec2>.Shared.Return(rented, clearArray: false);
        }
    }

    /// <summary>
    /// Converts a cubic B-spline exactly into one cubic Bezier segment per non-empty knot span.
    /// </summary>
    /// <remarks>
    /// Four exact curve samples are used per span. Because every cubic B-spline span is a cubic
    /// polynomial, the resulting Bezier representation is mathematically equivalent up to
    /// floating-point roundoff and remains valid even when interior knot multiplicities reduce
    /// continuity.
    /// </remarks>
    public IReadOnlyList<CubicBezier> ToBezierSegments()
    {
        if (Degree != 3)
            throw new NotSupportedException("Exact Bezier conversion currently supports cubic B-splines only.");

        var segments = new List<CubicBezier>(_controlPoints.Length - Degree);
        for (int span = Degree; span < _controlPoints.Length; span++)
        {
            double start = _knots[span];
            double end = _knots[span + 1];
            if (!(end > start))
                continue;

            double delta = end - start;
            var p0 = EvaluateSpanCore(_controlPoints, Degree, _knots, start, span);
            var q1 = EvaluateSpanCore(_controlPoints, Degree, _knots, start + delta / 3.0, span);
            var q2 = EvaluateSpanCore(_controlPoints, Degree, _knots, start + 2.0 * delta / 3.0, span);
            var p3 = EvaluateSpanCore(_controlPoints, Degree, _knots, end, span);

            // Invert the cubic Bernstein basis at t=1/3 and t=2/3.
            var a = q1 * 27.0 - p0 * 8.0 - p3;
            var b = q2 * 27.0 - p0 - p3 * 8.0;
            var p1 = (a * 2.0 - b) / 18.0;
            var p2 = (b * 2.0 - a) / 18.0;

            segments.Add(new CubicBezier(new[]
            {
                p0.X, p0.Y,
                p1.X, p1.Y,
                p2.X, p2.Y,
                p3.X, p3.Y,
            }));
        }

        return segments;
    }

    /// <summary>
    /// Converts a cubic B-spline to the package's Bezier path representation.
    /// Fully discontinuous interior knots start a new path subpath.
    /// </summary>
    public BezierPath ToBezierPath()
    {
        var path = new BezierPath();
        var segments = ToBezierSegments();
        if (segments.Count == 0)
            return path;

        int segmentIndex = 0;
        int previousSpan = -1;

        for (int span = Degree; span < _controlPoints.Length; span++)
        {
            double start = _knots[span];
            double end = _knots[span + 1];
            if (!(end > start))
                continue;

            var c = segments[segmentIndex++].C;

            // Consecutive non-empty spans are separated by exactly the knot
            // multiplicity. A multiplicity of degree + 1 (or greater) is a
            // C^-1 discontinuity, so the Bezier representation must begin a
            // new subpath rather than implicitly connecting the two spans.
            if (previousSpan < 0 || span - previousSpan >= Degree + 1)
                path.MoveTo(c[0], c[1]);

            path.CurveTo(c[2], c[3], c[4], c[5], c[6], c[7]);
            previousSpan = span;
        }

        return path;
    }

    /// <summary>
    /// Converts a cubic B-spline directly to SVG path syntax.
    /// </summary>
    public string ToSvgPath() => ToBezierPath().ToSvgPath();

    private void ValidateParameter(double parameter)
    {
        if (!IsFinite(parameter))
            throw new ArgumentOutOfRangeException(nameof(parameter), parameter, "Parameter must be finite.");
        if (parameter < DomainStart || parameter > DomainEnd)
            throw new ArgumentOutOfRangeException(
                nameof(parameter),
                parameter,
                $"Parameter must be in [{DomainStart}, {DomainEnd}].");
    }

    private static Vec2 EvaluateCore(
        ReadOnlySpan<Vec2> controlPoints,
        int degree,
        ReadOnlySpan<double> knots,
        double parameter)
    {
        int span = FindSpan(controlPoints.Length, degree, knots, parameter);
        return EvaluateSpanCore(controlPoints, degree, knots, parameter, span);
    }

    private static Vec2 EvaluateSpanCore(
        ReadOnlySpan<Vec2> controlPoints,
        int degree,
        ReadOnlySpan<double> knots,
        double parameter,
        int span)
    {
        Vec2[]? rented = null;
        Span<Vec2> work = degree <= StackDegreeThreshold
            ? stackalloc Vec2[degree + 1]
            : (rented = ArrayPool<Vec2>.Shared.Rent(degree + 1)).AsSpan(0, degree + 1);

        try
        {
            int first = span - degree;
            for (int j = 0; j <= degree; j++)
                work[j] = controlPoints[first + j];

            for (int level = 1; level <= degree; level++)
            {
                for (int j = degree; j >= level; j--)
                {
                    int i = span - degree + j;
                    double denominator = knots[i + degree - level + 1] - knots[i];
                    double alpha = denominator == 0 ? 0 : (parameter - knots[i]) / denominator;
                    work[j] = work[j - 1] * (1.0 - alpha) + work[j] * alpha;
                }
            }

            return work[degree];
        }
        finally
        {
            if (rented is not null)
                ArrayPool<Vec2>.Shared.Return(rented, clearArray: false);
        }
    }

    private static int FindSpan(
        int controlPointCount,
        int degree,
        ReadOnlySpan<double> knots,
        double parameter)
    {
        int lastControlPoint = controlPointCount - 1;
        if (parameter >= knots[controlPointCount])
            return lastControlPoint;

        int low = degree;
        int high = controlPointCount;
        int mid = (low + high) >> 1;

        while (parameter < knots[mid] || parameter >= knots[mid + 1])
        {
            if (parameter < knots[mid])
                high = mid;
            else
                low = mid;

            mid = (low + high) >> 1;
        }

        return mid;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
