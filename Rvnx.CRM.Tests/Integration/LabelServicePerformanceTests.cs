using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Infrastructure.Repositories;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Rvnx.CRM.Tests.Integration;

public class LabelServicePerformanceTests(ITestOutputHelper output) : SqliteIntegrationTestBase
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task GetAllAsyncPerformance()
    {
        Repository repository = new(Context);
        LabelService labelService = new(repository);

        int labelCount = 2000;
        List<Label> labels = [];
        for (int i = 0; i < labelCount; i++)
        {
            labels.Add(new Label
            {
                Id = Guid.NewGuid(),
                Name = $"Label {i}",
                Color = "#000000"
            });
        }
        await Context.Labels.AddRangeAsync(labels);
        await Context.SaveChangesAsync();

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<LabelDto> result = await labelService.GetAllAsync();
        stopwatch.Stop();

        Assert.Equal(labelCount, result.Count);
        _output.WriteLine($"GetAllAsync took: {stopwatch.ElapsedMilliseconds} ms for {labelCount} labels.");
    }
}