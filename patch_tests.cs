<<<<<<< SEARCH
    public class RelationshipServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly RelationshipService _service;

        public RelationshipServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new RelationshipService(_repositoryMock.Object);
        }

        [Fact]
=======
    public class RelationshipServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly RelationshipService _service;

        public RelationshipServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new RelationshipService(_repositoryMock.Object);
        }

        [Fact]
        public async Task GetSuggestedRelationshipsAsync_ReturnsSuggestions()
        {
            Guid sourceId = Guid.NewGuid();
            Guid targetId = Guid.NewGuid();
            Guid cId = Guid.NewGuid();
            Guid typeId = RelationshipTypeIds.Colleague;

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(sourceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Contact { Id = sourceId, FirstName = "Jack" });

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(targetId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Contact { Id = targetId, FirstName = "Jill" });

            _repositoryMock.Setup(r => r.GetByIdAsync<Contact>(cId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Contact { Id = cId, FirstName = "James" });

            // source has no relations
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.Is<Expression<Func<Relationship, bool>>>(expr => expr.Compile().Invoke(new Relationship { EntityId = sourceId, RelationshipTypeId = typeId })),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<Relationship>());

            // target has one relation with C
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(
                It.Is<Expression<Func<Relationship, bool>>>(expr => expr.Compile().Invoke(new Relationship { EntityId = targetId, RelationshipTypeId = typeId })),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                .ReturnsAsync(new List<Relationship> { new Relationship { EntityId = targetId, RelatedEntityId = cId, RelationshipTypeId = typeId } });

            var suggestions = await _service.GetSuggestedRelationshipsAsync(sourceId, targetId, typeId, null);

            Assert.Single(suggestions);
            Assert.Equal(cId, suggestions[0].ExistingContactId);
            Assert.Equal("Jack", suggestions[0].SourceName);
            Assert.Equal("James", suggestions[0].TargetName);
        }

        [Fact]
>>>>>>> REPLACE
