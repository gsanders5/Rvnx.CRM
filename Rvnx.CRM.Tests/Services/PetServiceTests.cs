using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

public class PetServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly PetService _service;

    public PetServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new PetService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsyncWhenContactNotFoundReturnsNotFoundResult()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        PetFormDto dto = new() { ContactId = contactId, Name = "Buddy", Species = "Dog" };

        _repositoryMock.Setup(r => r.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        OperationResult result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<PetContact>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncWhenPetContactsRequireChange()
    {
        // Arrange
        Guid petId = Guid.NewGuid();
        Guid contactA = Guid.NewGuid();
        Guid contactB = Guid.NewGuid();
        Guid contactC = Guid.NewGuid();

        PetContact pcA = new() { PetId = petId, ContactId = contactA };
        PetContact pcB = new() { PetId = petId, ContactId = contactB };

        Pet existingPet = new()
        {
            Id = petId,
            Name = "Whiskers",
            PetContacts = [pcA, pcB]
        };

        PetFormDto dto = new()
        {
            Id = petId,
            ContactId = contactB,
            ContactIds = [contactB, contactC],
            Name = "Whiskers"
        };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Pet>(petId, nameof(Pet.PetContacts)))
            .ReturnsAsync(existingPet);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPet);

        _repositoryMock.Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<PetContact>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PetContact>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<PetContact> entities, CancellationToken _) => entities);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        OperationResult result = await _service.UpdateAsync(petId, dto);

        // Assert
        Assert.True(result.Success);
        _repositoryMock.Verify(r => r.DeleteRangeAsync(
            It.Is<IEnumerable<PetContact>>(list => list.Any(pc => pc.ContactId == contactA)),
            It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<PetContact>>(list => list.Any(pc => pc.ContactId == contactC)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByContactAsyncWithMultiplePetsReturnsMappedDtos()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();

        List<PetContact> petContacts =
        [
            new PetContact { ContactId = contactId, Pet = new Pet { Name = "Rex", Species = "Dog" } },
            new PetContact { ContactId = contactId, Pet = new Pet { Name = "Mittens", Species = "Cat" } },
            new PetContact { ContactId = contactId, Pet = new Pet { Name = "Goldie", Species = "Fish" } }
        ];

        _repositoryMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PetContact, bool>>>(),
                It.IsAny<CancellationToken>(),
                nameof(PetContact.Pet)))
            .ReturnsAsync(petContacts);

        // Act
        List<PetDto> result = await _service.GetByContactAsync(contactId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, p => p.Name == "Rex");
        Assert.Contains(result, p => p.Name == "Mittens");
        Assert.Contains(result, p => p.Name == "Goldie");
    }

    [Fact]
    public async Task CreateAsyncWhenPrimaryOwnerIsDeceasedReturnsFailure()
    {
        // Arrange — registering a NEW pet against a deceased owner is forward-looking and refused.
        Guid contactId = Guid.NewGuid();
        PetFormDto dto = new() { ContactId = contactId, Name = "Rex", Species = "Dog" };

        Contact deceased = new()
        {
            Id = contactId,
            FirstName = "Late",
            IsPartial = false,
            IsDeceased = true
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Contact, bool>> filter, CancellationToken _) =>
                filter.Compile()(deceased) ? 1 : 0);

        // Act
        OperationResult result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("deceased", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFormForCreateAsyncWhenPrimaryOwnerIsDeceasedReturnsNull()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();

        Contact deceased = new()
        {
            Id = contactId,
            FirstName = "Late",
            IsPartial = false,
            IsDeceased = true
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Contact, bool>> filter, CancellationToken _) =>
                filter.Compile()(deceased) ? 1 : 0);

        // Act
        PetFormDto? result = await _service.GetFormForCreateAsync(contactId);

        // Assert
        Assert.Null(result);
    }
}
