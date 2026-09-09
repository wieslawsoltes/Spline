using System;
using Spline;

var fitPoints = new[]
{
    new Vec2(0, 0),
    new Vec2(40, 70),
    new Vec2(90, 100),
    new Vec2(150, 75),
    new Vec2(210, 20),
    new Vec2(280, 0),
};

// In Direction mode only the vector direction matters. The solver estimates
// derivative magnitude from the total chord length, then applies the scale.
var result = GlobalBSpline.InterpolateDetailed(
    fitPoints,
    startTangent: new Vec2(1.0, 0.35),
    endTangent: new Vec2(1.0, -0.20),
    options: new GlobalBSplineOptions
    {
        Parameterization = BSplineParameterization.ChordLength,
        TangentMode = BSplineTangentMode.Direction,
    });

var curve = result.Curve;

Console.WriteLine($"Degree: {curve.Degree}");
Console.WriteLine($"Control points: {curve.ControlPoints.Count}");
Console.WriteLine($"Knots: {curve.Knots.Count}");
Console.WriteLine($"Total chord length: {result.TotalChordLength:R}");
Console.WriteLine();

for (int i = 0; i < fitPoints.Length; i++)
{
    double u = result.FitParameters[i];
    var evaluated = curve.Evaluate(u);
    Console.WriteLine($"Q{i}: u={u:R}, C(u)=({evaluated.X:R}, {evaluated.Y:R})");
}

Console.WriteLine();
Console.WriteLine("Control net:");
for (int i = 0; i < curve.ControlPoints.Count; i++)
{
    var point = curve.ControlPoints[i];
    Console.WriteLine($"P{i}: ({point.X:R}, {point.Y:R})");
}

Console.WriteLine();
Console.WriteLine("Knot vector:");
for (int i = 0; i < curve.Knots.Count; i++)
    Console.WriteLine($"U{i}: {curve.Knots[i]:R}");

Console.WriteLine();
Console.WriteLine("SVG path:");
Console.WriteLine(curve.ToSvgPath());
