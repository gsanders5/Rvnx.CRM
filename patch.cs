<<<<<<< SEARCH
    public static readonly Guid Acquaintance = Guid.Parse("71186e94-7048-4b3c-a854-5482328ab505");

    public static readonly Guid ParentCompany = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210");
}

public static class RelationshipTypeService
=======
    public static readonly Guid Acquaintance = Guid.Parse("71186e94-7048-4b3c-a854-5482328ab505");

    public static readonly Guid ParentCompany = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210");
}

public static class RelationshipTypeService
{
    public static readonly IReadOnlySet<Guid> TransitiveRelationshipTypeIds = new HashSet<Guid>
    {
        RelationshipTypeIds.Sibling,
        RelationshipTypeIds.Cousin,
        RelationshipTypeIds.StepSibling,
        RelationshipTypeIds.HalfSibling,
        RelationshipTypeIds.Colleague,
        RelationshipTypeIds.BusinessPartner
    };

>>>>>>> REPLACE
