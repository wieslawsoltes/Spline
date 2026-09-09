using System;
using System.Collections.Generic;

namespace Spline;

/// <summary>
/// Selects how fit points are mapped to the normalized B-spline parameter domain.
/// </summary>
public enum BSplineParameterization
{
    /// <summary>
    /// Parameter increments are proportional to Euclidean chord length.
    /// </summary>
    ChordLength,

    /// <summary>
    /// Parameter increments are proportional to the square root of chord length.
    /// This often reduces overshoot for unevenly distributed fit points.
    /// </summary>
    Centripetal,

    /// <summary>
    /// Fit points are assigned equally spaced parameters.
    /// </summary>
    Uniform,
}

/// <summary>
/// Selects how endpoint tangent vectors are interpreted during interpolation.
/// </summary>
public enum BSplineTangentMode
{
    /// <summary>
    /// Only tangent direction is used. The derivative magnitude is estimated from total chord length.
    /// </summary>
    Direction,

    /// <summary>
    /// The supplied vectors are first derivatives with respect to the normalized spline parameter.
    /// </summary>
    Derivative,
}

/// <summary>
/// Configures global cubic B-spline interpolation.
/// </summary>
public sealed class GlobalBSplineOptions
{
    /// <summary>
    /// Gets or sets fit-point parameterization. The default is chord length.
    /// </summary>
    public BSplineParameterization Parameterization { get; init; } = BSplineParameterization.ChordLength;

    /// <summary>
    /// Gets or sets endpoint tangent interpretation. The default uses direction only.
    /// </summary>
    public BSplineTangentMode TangentMode { get; init; } = BSplineTangentMode.Direction;

    /// <summary>
    /// Gets or sets a multiplier applied to the resolved start derivative. Must be positive and finite.
    /// </summary>
    public double StartTangentScale { get; init; } = 1.0;

    /// <summary>
    /// Gets or sets a multiplier applied to the resolved end derivative. Must be positive and finite.
    /// </summary>
    public double EndTangentScale { get; init; } = 1.0;

    /// <summary>
    /// Gets or sets the relative threshold used to reject effectively coincident consecutive fit points.
    /// The threshold is multiplied by total chord length. The default is 1e-12.
    /// </summary>
    public double RelativePointTolerance { get; init; } = 1e-12;

    /// <summary>
    /// Gets or sets the absolute pivot threshold used by the tridiagonal solver. The default is 1e-14.
    /// </summary>
    public double SolverPivotTolerance { get; init; } = 1e-14;
}

/// <summary>
/// Contains a global interpolation result together with the parameter and derivative data used to build it.
/// </summary>
public sealed class GlobalBSplineResult
{
    private readonly double[] _fitParameters;
    private readonly IReadOnlyList<double> _fitParametersView;

    internal GlobalBSplineResult(
        BSplineCurve curve,
        double[] fitParameters,
        Vec2 startDerivative,
        Vec2 endDerivative,
        double totalChordLength,
        BSplineParameterization parameterization,
        BSplineTangentMode tangentMode)
    {
        Curve = curve;
        _fitParameters = fitParameters;
        _fitParametersView = Array.AsReadOnly(_fitParameters);
        StartDerivative = startDerivative;
        EndDerivative = endDerivative;
        TotalChordLength = totalChordLength;
        Parameterization = parameterization;
        TangentMode = tangentMode;
    }

    /// <summary>
    /// Gets the resulting cubic B-spline curve.
    /// </summary>
    public BSplineCurve Curve { get; }

    /// <summary>
    /// Gets the normalized parameter assigned to each input fit point.
    /// </summary>
    public IReadOnlyList<double> FitParameters => _fitParametersView;

    /// <summary>
    /// Gets the first derivative enforced at the start of the curve.
    /// </summary>
    public Vec2 StartDerivative { get; }

    /// <summary>
    /// Gets the first derivative enforced at the end of the curve.
    /// </summary>
    public Vec2 EndDerivative { get; }

    /// <summary>
    /// Gets the sum of Euclidean distances between consecutive fit points.
    /// </summary>
    public double TotalChordLength { get; }

    /// <summary>
    /// Gets the parameterization used for the solve.
    /// </summary>
    public BSplineParameterization Parameterization { get; }

    /// <summary>
    /// Gets the endpoint tangent interpretation used for the solve.
    /// </summary>
    public BSplineTangentMode TangentMode { get; }
}

/// <summary>
/// Builds a globally interpolating open cubic B-spline from fit points and endpoint tangent constraints.
/// </summary>
/// <remarks>
/// The implementation uses a clamped cubic B-spline with the fit parameters as simple interior knots.
/// Endpoint derivative constraints determine the first and last inner control points directly. The
/// remaining control points form a tridiagonal system, so construction is O(n) in fit-point count and
/// requires no general-purpose matrix package.
/// </remarks>
public static class GlobalBSpline
{
    private const int CubicDegree = 3;

    /// <summary>
    /// Interpolates fit points with an open cubic B-spline.
    /// </summary>
    /// <param name="fitPoints">Points that the resulting curve must pass through.</param>
    /// <param name="startTangent">Start tangent direction or derivative, depending on options.</param>
    /// <param name="endTangent">End tangent direction or derivative, depending on options.</param>
    /// <param name="degree">Spline degree. Production interpolation currently supports cubic degree 3.</param>
    /// <param name="options">Optional parameterization, tangent, and numerical settings.</param>
    public static BSplineCurve Interpolate(
        IReadOnlyList<Vec2> fitPoints,
        Vec2 startTangent,
        Vec2 endTangent,
        int degree = CubicDegree,
        GlobalBSplineOptions? options = null)
        => InterpolateDetailed(fitPoints, startTangent, endTangent, degree, options).Curve;

    /// <summary>
    /// Interpolates fit points and returns the curve together with its fit parameters and effective derivatives.
    /// </summary>
    public static GlobalBSplineResult InterpolateDetailed(
        IReadOnlyList<Vec2> fitPoints,
        Vec2 startTangent,
        Vec2 endTangent,
        int degree = CubicDegree,
        GlobalBSplineOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(fitPoints);
        options ??= new GlobalBSplineOptions();

        if (degree != CubicDegree)
            throw new NotSupportedException("Global interpolation currently supports degree 3 only.");
        if (fitPoints.Count < 2)
            throw new ArgumentException("At least two fit points are required.", nameof(fitPoints));

        ValidateOptions(options);
        ValidateTangent(startTangent, nameof(startTangent));
        ValidateTangent(endTangent, nameof(endTangent));

        int pointCount = fitPoints.Count;
        int n = pointCount - 1;
        var distances = new double[n];
        double totalChordLength = 0;

        for (int i = 0; i < pointCount; i++)
        {
            var point = fitPoints[i];
            if (!IsFinite(point.X) || !IsFinite(point.Y))
                throw new ArgumentException($"Fit point {i} contains a non-finite coordinate.", nameof(fitPoints));

            if (i == 0)
                continue;

            double distance = Distance(fitPoints[i - 1], point);
            if (!IsFinite(distance))
                throw new ArgumentException("Fit-point distances overflow the supported numeric range.", nameof(fitPoints));

            distances[i - 1] = distance;
            totalChordLength += distance;
        }

        if (!(totalChordLength > 0) || !IsFinite(totalChordLength))
            throw new ArgumentException("Fit points must span a finite, non-zero total chord length.", nameof(fitPoints));

        double minimumSegmentLength = options.RelativePointTolerance * totalChordLength;
        for (int i = 0; i < distances.Length; i++)
        {
            if (!(distances[i] > minimumSegmentLength))
            {
                throw new ArgumentException(
                    $"Fit points {i} and {i + 1} are coincident or too close relative to the complete data set.",
                    nameof(fitPoints));
            }
        }

        var parameters = Parameterize(distances, options.Parameterization);
        var startDerivative = ResolveDerivative(
            startTangent,
            options.TangentMode,
            totalChordLength,
            options.StartTangentScale);
        var endDerivative = ResolveDerivative(
            endTangent,
            options.TangentMode,
            totalChordLength,
            options.EndTangentScale);

        var knots = BuildCubicKnotVector(parameters);
        var controlPoints = SolveCubicControlPoints(
            fitPoints,
            parameters,
            knots,
            startDerivative,
            endDerivative,
            options.SolverPivotTolerance);

        var curve = new BSplineCurve(CubicDegree, controlPoints, knots);
        return new GlobalBSplineResult(
            curve,
            parameters,
            startDerivative,
            endDerivative,
            totalChordLength,
            options.Parameterization,
            options.TangentMode);
    }

    private static double[] Parameterize(double[] distances, BSplineParameterization parameterization)
    {
        int pointCount = distances.Length + 1;
        var parameters = new double[pointCount];
        var increments = new double[distances.Length];
        double total = 0;

        for (int i = 0; i < distances.Length; i++)
        {
            double increment = parameterization switch
            {
                BSplineParameterization.ChordLength => distances[i],
                BSplineParameterization.Centripetal => Math.Sqrt(distances[i]),
                BSplineParameterization.Uniform => 1.0,
                _ => throw new ArgumentOutOfRangeException(nameof(parameterization)),
            };

            increments[i] = increment;
            total += increment;
        }

        if (!(total > 0) || !IsFinite(total))
            throw new InvalidOperationException("Failed to construct a finite parameterization.");

        double cumulative = 0;
        parameters[0] = 0;
        for (int i = 1; i < pointCount - 1; i++)
        {
            cumulative += increments[i - 1];
            parameters[i] = cumulative / total;
            if (!(parameters[i] > parameters[i - 1]) || !(parameters[i] < 1.0))
                throw new InvalidOperationException("Parameterization lost strict monotonicity at machine precision.");
        }

        parameters[^1] = 1.0;
        return parameters;
    }

    private static double[] BuildCubicKnotVector(double[] parameters)
    {
        int pointCount = parameters.Length;
        int n = pointCount - 1;
        int controlPointCount = pointCount + 2;
        var knots = new double[controlPointCount + CubicDegree + 1];

        // Four start knots are already zero. Each interior fit parameter becomes a simple knot.
        for (int fitIndex = 1; fitIndex < n; fitIndex++)
            knots[fitIndex + CubicDegree] = parameters[fitIndex];

        // Four clamped end knots.
        for (int i = n + CubicDegree; i < knots.Length; i++)
            knots[i] = 1.0;

        return knots;
    }

    private static Vec2[] SolveCubicControlPoints(
        IReadOnlyList<Vec2> fitPoints,
        double[] parameters,
        double[] knots,
        Vec2 startDerivative,
        Vec2 endDerivative,
        double pivotTolerance)
    {
        int n = fitPoints.Count - 1;
        var controlPoints = new Vec2[fitPoints.Count + 2];

        controlPoints[0] = fitPoints[0];
        controlPoints[n + 2] = fitPoints[n];

        // C'(0) = 3 / U4 * (P1 - P0)
        controlPoints[1] = controlPoints[0] + startDerivative * (knots[4] / CubicDegree);

        // C'(1) = 3 / (1 - U[n+2]) * (P[n+2] - P[n+1])
        controlPoints[n + 1] = controlPoints[n + 2]
                             - endDerivative * ((1.0 - knots[n + 2]) / CubicDegree);

        int unknownCount = n - 1; // P2 ... Pn
        if (unknownCount <= 0)
            return controlPoints;

        var lower = new double[Math.Max(0, unknownCount - 1)];
        var diagonal = new double[unknownCount];
        var upper = new double[Math.Max(0, unknownCount - 1)];
        var rightHandSide = new Vec2[unknownCount];
        Span<double> basis = stackalloc double[CubicDegree + 1];

        for (int fitIndex = 1; fitIndex < n; fitIndex++)
        {
            int row = fitIndex - 1;
            int span = fitIndex + CubicDegree;
            EvaluateBasisFunctions(span, parameters[fitIndex], CubicDegree, knots, basis);

            double a = basis[0];
            double b = basis[1];
            double c = basis[2];
            var rhs = fitPoints[fitIndex];

            if (fitIndex == 1)
                rhs -= controlPoints[1] * a;
            else
                lower[row - 1] = a;

            diagonal[row] = b;

            if (fitIndex == n - 1)
                rhs -= controlPoints[n + 1] * c;
            else
                upper[row] = c;

            rightHandSide[row] = rhs;
        }

        SolveTridiagonalInPlace(lower, diagonal, upper, rightHandSide, pivotTolerance);
        for (int i = 0; i < unknownCount; i++)
            controlPoints[i + 2] = rightHandSide[i];

        return controlPoints;
    }

    private static void SolveTridiagonalInPlace(
        double[] lower,
        double[] diagonal,
        double[] upper,
        Vec2[] rightHandSide,
        double pivotTolerance)
    {
        int count = diagonal.Length;
        if (count == 0)
            return;

        for (int row = 1; row < count; row++)
        {
            EnsurePivot(diagonal[row - 1], row - 1, pivotTolerance);
            double factor = lower[row - 1] / diagonal[row - 1];
            diagonal[row] -= factor * upper[row - 1];
            rightHandSide[row] -= rightHandSide[row - 1] * factor;
        }

        EnsurePivot(diagonal[^1], count - 1, pivotTolerance);
        rightHandSide[^1] /= diagonal[^1];

        for (int row = count - 2; row >= 0; row--)
        {
            EnsurePivot(diagonal[row], row, pivotTolerance);
            rightHandSide[row] = (rightHandSide[row] - rightHandSide[row + 1] * upper[row]) / diagonal[row];
        }
    }

    private static void EvaluateBasisFunctions(
        int span,
        double parameter,
        int degree,
        double[] knots,
        Span<double> destination)
    {
        destination[..(degree + 1)].Clear();
        Span<double> left = stackalloc double[degree + 1];
        Span<double> right = stackalloc double[degree + 1];
        destination[0] = 1.0;

        for (int j = 1; j <= degree; j++)
        {
            left[j] = parameter - knots[span + 1 - j];
            right[j] = knots[span + j] - parameter;
            double saved = 0;

            for (int r = 0; r < j; r++)
            {
                double denominator = right[r + 1] + left[j - r];
                double temp = denominator == 0 ? 0 : destination[r] / denominator;
                destination[r] = saved + right[r + 1] * temp;
                saved = left[j - r] * temp;
            }

            destination[j] = saved;
        }
    }

    private static Vec2 ResolveDerivative(
        Vec2 tangent,
        BSplineTangentMode tangentMode,
        double totalChordLength,
        double scale)
    {
        Vec2 derivative;
        if (tangentMode == BSplineTangentMode.Derivative)
        {
            derivative = tangent * scale;
        }
        else
        {
            double length = Length(tangent.X, tangent.Y);
            derivative = tangent * (totalChordLength * scale / length);
        }

        if (!IsFinite(derivative.X) || !IsFinite(derivative.Y))
            throw new ArgumentOutOfRangeException(nameof(scale), "Resolved tangent derivative exceeds the supported numeric range.");

        return derivative;
    }

    private static void ValidateOptions(GlobalBSplineOptions options)
    {
        if (!Enum.IsDefined(typeof(BSplineParameterization), options.Parameterization))
            throw new ArgumentOutOfRangeException(nameof(options.Parameterization));
        if (!Enum.IsDefined(typeof(BSplineTangentMode), options.TangentMode))
            throw new ArgumentOutOfRangeException(nameof(options.TangentMode));
        if (!(options.StartTangentScale > 0) || !IsFinite(options.StartTangentScale))
            throw new ArgumentOutOfRangeException(nameof(options.StartTangentScale), "Start tangent scale must be positive and finite.");
        if (!(options.EndTangentScale > 0) || !IsFinite(options.EndTangentScale))
            throw new ArgumentOutOfRangeException(nameof(options.EndTangentScale), "End tangent scale must be positive and finite.");
        if (!(options.RelativePointTolerance >= 0) || !IsFinite(options.RelativePointTolerance))
            throw new ArgumentOutOfRangeException(nameof(options.RelativePointTolerance), "Relative point tolerance must be non-negative and finite.");
        if (!(options.SolverPivotTolerance > 0) || !IsFinite(options.SolverPivotTolerance))
            throw new ArgumentOutOfRangeException(nameof(options.SolverPivotTolerance), "Solver pivot tolerance must be positive and finite.");
    }

    private static void ValidateTangent(Vec2 tangent, string parameterName)
    {
        if (!IsFinite(tangent.X) || !IsFinite(tangent.Y))
            throw new ArgumentException("Tangent must contain finite coordinates.", parameterName);
        if (!(Length(tangent.X, tangent.Y) > 0))
            throw new ArgumentException("Tangent must be non-zero.", parameterName);
    }

    private static void EnsurePivot(double pivot, int row, double tolerance)
    {
        if (!IsFinite(pivot) || Math.Abs(pivot) <= tolerance)
        {
            throw new InvalidOperationException(
                $"The global B-spline interpolation system is singular or ill-conditioned near row {row}.");
        }
    }

    private static double Distance(Vec2 a, Vec2 b) => Length(b.X - a.X, b.Y - a.Y);

    private static double Length(double x, double y)
    {
        double ax = Math.Abs(x);
        double ay = Math.Abs(y);
        double max = Math.Max(ax, ay);
        if (max == 0)
            return 0;

        double min = Math.Min(ax, ay);
        double ratio = min / max;
        return max * Math.Sqrt(1.0 + ratio * ratio);
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
