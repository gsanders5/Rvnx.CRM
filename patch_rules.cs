<<<<<<< SEARCH
    public static readonly IReadOnlySet<Guid> TransitiveRelationshipTypeIds = new HashSet<Guid>
    {
        RelationshipTypeIds.Sibling,
        RelationshipTypeIds.Cousin,
        RelationshipTypeIds.StepSibling,
        RelationshipTypeIds.HalfSibling,
        RelationshipTypeIds.Colleague,
        RelationshipTypeIds.BusinessPartner
    };
=======
    public static readonly IReadOnlySet<Guid> TransitiveRelationshipTypeIds = new HashSet<Guid>
    {
        RelationshipTypeIds.Sibling,
        RelationshipTypeIds.Cousin,
        RelationshipTypeIds.StepSibling,
        RelationshipTypeIds.HalfSibling,
        RelationshipTypeIds.Colleague,
        RelationshipTypeIds.BusinessPartner
    };

    public static readonly IReadOnlySet<Guid> FamilyAdultChildRelationshipTypeIds = new HashSet<Guid>
    {
        RelationshipTypeIds.Parent,
        RelationshipTypeIds.Grandparent,
        RelationshipTypeIds.UncleAunt,
        RelationshipTypeIds.Godparent,
        RelationshipTypeIds.StepParent
    };
>>>>>>> REPLACE
