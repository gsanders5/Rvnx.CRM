using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Web.ViewModels.Contact;

public record class RelationshipDeleteViewModel : RelationshipDto
{
    public string? ReturnUrl { get; init; }

    public RelationshipDeleteViewModel(RelationshipDto dto, string? returnUrl) : base(dto)
    {
        ReturnUrl = returnUrl;
    }
}
