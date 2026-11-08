using System.Diagnostics;
using System.Text;

namespace MdViewer.Tests;

/// <summary>Very small assertion harness: no packages, deterministic output.</summary>
public static class Harness
{
    private static int _passed;
    private static int _failed;
    private static readonly List<string> Failures = [];
    private static readonly Dictionary<string, int> SuiteTotals = new(StringComparer.Ordinal);
    private static string _suite = string.Empty;

    public static bool Verbose { get; set; }

    public static int Passed => _passed;

    public static int Failed => _failed;

    public static void Suite(string name)
    {
        _suite = name;
        Console.WriteLine();
        Console.WriteLine($"■ {name}");
    }

    public static void Test(string name, Action body)
    {
        SuiteTotals[_suite] = SuiteTotals.GetValueOrDefault(_suite) + 1;
        var sw = Stopwatch.StartNew();
        try
        {
            body();
            sw.Stop();
            _passed++;
            if (Verbose)
            {
                Console.WriteLine($"  ✔ {name} ({sw.Elapsed.TotalMilliseconds:F1} ms)");
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            _failed++;
            string message = ex is AssertionException ? ex.Message : ex.ToString();
            Failures.Add($"{_suite} / {name}: {message}");
            Console.WriteLine($"  ✘ {name}");
            Console.WriteLine($"      {message.Replace("\n", "\n      ")}");
        }
    }

    public static void True(bool condition, string what)
    {
        if (!condition)
        {
            throw new AssertionException($"expected true: {what}");
        }
    }

    public static void False(bool condition, string what) => True(!condition, what);

    public static void Equal(string expected, string actual, string what)
    {
        if (expected != actual)
        {
            throw new AssertionException($"{what}\n  expected: {Show(expected)}\n  actual  : {Show(actual)}");
        }
    }

    public static void Equal(int expected, int actual, string what)
    {
        if (expected != actual)
        {
            throw new AssertionException($"{what}: expected {expected}, actual {actual}");
        }
    }

    public static void Equal<T>(T expected, T actual, string what)
        where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertionException($"{what}: expected {expected}, actual {actual}");
        }
    }

    public static void Contains(string haystack, string needle, string what)
    {
        if (!haystack.Contains(needle, StringComparison.Ordinal))
        {
            throw new AssertionException($"{what}: '{needle}' not found in:\n{Show(haystack)}");
        }
    }

    public static void NotContains(string haystack, string needle, string what)
    {
        if (haystack.Contains(needle, StringComparison.Ordinal))
        {
            throw new AssertionException($"{what}: '{needle}' unexpectedly present in:\n{Show(haystack)}");
        }
    }

    private static string Show(string value) =>
        value.Length <= 900 ? value.Replace("\r", "\\r") : value[..900] + "…";

    public static int Summarize()
    {
        Console.WriteLine();
        foreach ((string suite, int count) in SuiteTotals)
        {
            Console.WriteLine($"  {suite,-22} {count}");
        }

        Console.WriteLine();
        if (_failed == 0)
        {
            Console.WriteLine($"ALL PASSED — {_passed} checks");
            return 0;
        }

        Console.WriteLine($"FAILED — {_failed} of {_passed + _failed} checks");
        foreach (string failure in Failures)
        {
            Console.WriteLine("  • " + failure.Replace("\n", "\n    "));
        }

        return 1;
    }
}

public sealed class AssertionException(string message) : Exception(message);

