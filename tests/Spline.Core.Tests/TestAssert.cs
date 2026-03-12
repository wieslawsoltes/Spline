using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Xunit.Sdk;

namespace Spline.Core.Tests;

internal static class TestAssert
{
    public static void EqualWithin(double expected, double actual, double tolerance = 1e-12, string? label = null)
    {
        if (double.IsNaN(expected) || double.IsInfinity(expected))
        {
            if (!expected.Equals(actual))
            {
                throw new XunitException($"{label ?? "value"} expected {expected} but was {actual}.");
            }

            return;
        }

        double delta = Math.Abs(expected - actual);
        if (delta > tolerance)
        {
            throw new XunitException($"{label ?? "value"} expected {expected} +/- {tolerance} but was {actual} (delta {delta}).");
        }
    }

    public static void VecEqualWithin(Vec2 expected, Vec2 actual, double tolerance = 1e-12, string? label = null)
    {
        EqualWithin(expected.X, actual.X, tolerance, $"{label ?? "vec"}.X");
        EqualWithin(expected.Y, actual.Y, tolerance, $"{label ?? "vec"}.Y");
    }

    public static void ArrayEqualWithin(double[] expected, double[] actual, double tolerance = 1e-12, string? label = null)
    {
        if (expected.Length != actual.Length)
        {
            throw new XunitException($"{label ?? "array"} length expected {expected.Length} but was {actual.Length}.");
        }

        for (int i = 0; i < expected.Length; i++)
        {
            EqualWithin(expected[i], actual[i], tolerance, $"{label ?? "array"}[{i}]");
        }
    }

    public static void SvgPathEqualWithin(string expected, string actual, double tolerance = 1e-12, string? label = null)
    {
        string expectedShape = Regex.Replace(expected, @"-?\d+(?:\.\d+)?(?:E[+-]?\d+)?", "#");
        string actualShape = Regex.Replace(actual, @"-?\d+(?:\.\d+)?(?:E[+-]?\d+)?", "#");
        if (!string.Equals(expectedShape, actualShape, StringComparison.Ordinal))
        {
            throw new XunitException($"{label ?? "svg"} command structure expected '{expectedShape}' but was '{actualShape}'.");
        }

        var expectedMatches = Regex.Matches(expected, @"-?\d+(?:\.\d+)?(?:E[+-]?\d+)?");
        var actualMatches = Regex.Matches(actual, @"-?\d+(?:\.\d+)?(?:E[+-]?\d+)?");
        if (expectedMatches.Count != actualMatches.Count)
        {
            throw new XunitException($"{label ?? "svg"} numeric count expected {expectedMatches.Count} but was {actualMatches.Count}.");
        }

        for (int i = 0; i < expectedMatches.Count; i++)
        {
            double expectedValue = double.Parse(expectedMatches[i].Value, CultureInfo.InvariantCulture);
            double actualValue = double.Parse(actualMatches[i].Value, CultureInfo.InvariantCulture);
            EqualWithin(expectedValue, actualValue, tolerance, $"{label ?? "svg"}[{i}]");
        }
    }
}
