using System;
using System.Collections.Generic;
using System.Linq;

namespace Rvnx.CRM.Tests.Services;

public class DashboardServiceBenchmark
{
    public void BenchmarkGroupToDictionary()
    {
        var profileAttachments = new List<(Guid ContactId, Guid AttachmentId)>();
        var r = new Random(42);

        for (int i = 0; i < 10000; i++)
        {
            profileAttachments.Add((Guid.NewGuid(), Guid.NewGuid()));
        }

        // Before
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            var map1 = profileAttachments
                .GroupBy(a => a.ContactId)
                .ToDictionary(g => g.Key, g => g.First().AttachmentId);
        }
        sw1.Stop();

        // After
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            var map2 = new Dictionary<Guid, Guid>(profileAttachments.Count);
            foreach (var item in profileAttachments)
            {
                map2.TryAdd(item.ContactId, item.AttachmentId);
            }
        }
        sw2.Stop();

        Console.WriteLine($"Before: {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"After: {sw2.ElapsedMilliseconds} ms");
    }
}
