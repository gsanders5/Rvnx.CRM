using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Linq;

public class Benchmark
{
    private static readonly List<string> ListTypes = new List<string> { "image/jpeg", "image/png", "image/gif" };
    private static readonly HashSet<string> HashSetTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/gif" };
    private static readonly FrozenSet<string> FrozenSetTypes = new[] { "image/jpeg", "image/png", "image/gif" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static void Main()
    {
        string lookup = "image/png";
        int iterations = 100_000_000;

        // Warmup
        ListTypes.Contains(lookup);
        HashSetTypes.Contains(lookup);
        FrozenSetTypes.Contains(lookup);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            bool b = ListTypes.Contains(lookup, StringComparer.OrdinalIgnoreCase);
        }
        sw.Stop();
        Console.WriteLine($"List (OrdinalIgnoreCase): {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            bool b = HashSetTypes.Contains(lookup);
        }
        sw.Stop();
        Console.WriteLine($"HashSet (OrdinalIgnoreCase): {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            bool b = FrozenSetTypes.Contains(lookup);
        }
        sw.Stop();
        Console.WriteLine($"FrozenSet (OrdinalIgnoreCase): {sw.ElapsedMilliseconds} ms");

        // Test with manual loop for List to simulate simple Contains without comparer overhead if using default
        // But we need case insensitive.
        // List.Contains with StringComparer is extension method from System.Linq? No, List<T>.Contains(T) uses default comparer.
        // To use custom comparer with List, we use Enumerable.Contains extension method.

        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
             // This uses Enumerable.Contains
            bool b = Enumerable.Contains(ListTypes, lookup, StringComparer.OrdinalIgnoreCase);
        }
        sw.Stop();
        Console.WriteLine($"Enumerable.Contains on List: {sw.ElapsedMilliseconds} ms");
    }
}
