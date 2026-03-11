using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;

class Program
{
    static async Task Main(string[] args)
    {
        int numSuggested = 50;
        Console.WriteLine($"Benchmarking with {numSuggested} suggested relationships...");

        var mockRepo = new Mock<IRepository>();

        // Simulate DB delay
        mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(async () => { await Task.Delay(2); return 0; });

        mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Relationship, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(async () => { await Task.Delay(2); return new List<Relationship>(); });

        mockRepo.Setup(r => r.AddAsync(It.IsAny<Relationship>(), It.IsAny<CancellationToken>()))
            .Returns(async () => { await Task.Delay(0); return new Relationship(); });

        var service = new RelationshipService(mockRepo.Object);

        var rel = new Relationship { EntityId = Guid.NewGuid(), RelatedEntityId = Guid.NewGuid(), EntityType = EntityTypes.Person };
        var typeId = Guid.NewGuid();
        var selectedType = $"{typeId}_Fwd";

        var suggestions = new List<string>();
        for (int i = 0; i < numSuggested; i++)
        {
            suggestions.Add($"{Guid.NewGuid()}_{Guid.NewGuid()}_False");
        }

        var sw = Stopwatch.StartNew();
        await service.CreateRelationshipAsync(rel, selectedType, suggestions);
        sw.Stop();

        Console.WriteLine($"CreateRelationshipAsync took {sw.ElapsedMilliseconds} ms");
    }
}
