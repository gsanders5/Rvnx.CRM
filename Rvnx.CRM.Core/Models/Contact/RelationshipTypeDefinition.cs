namespace Rvnx.CRM.Core.Models.Contact;

public record RelationshipTypeDefinition(Guid Id, string Name, string OppositeName, string Category, string EntityType)
{
    public bool IsSymmetric => Name == OppositeName;
}
