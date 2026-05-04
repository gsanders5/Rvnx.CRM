using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Dates;

namespace Rvnx.CRM.Core.DTOs.Contact;

public record class ContactDetailDto : ContactDto
{
    public string? Nickname { get; set; }

    public IEnumerable<NoteDto> Notes { get; set; } = [];
    public IEnumerable<SignificantDateDto> SignificantDates { get; set; } = [];
    public IEnumerable<RelationshipDto> Relationships { get; set; } = [];
    public IEnumerable<RelationshipDto> RelatedTo { get; set; } = [];
    public IEnumerable<PetDto> Pets { get; set; } = [];
    public IEnumerable<ContactMethodDto> ContactMethods { get; set; } = [];
    public IEnumerable<FactDto> Facts { get; set; } = [];
    public IEnumerable<AttachmentDto> Attachments { get; set; } = [];
    public IEnumerable<AddressDto> Addresses { get; set; } = [];
    public IEnumerable<ActivityDto> Activities { get; set; } = [];
    public IEnumerable<ContactTaskDto> ContactTasks { get; set; } = [];

    public Guid? ImmichPersonId { get; set; }
    public string? ImmichPersonName { get; set; }
    public Guid? ImmichTagId { get; set; }
    public string? ImmichTagValue { get; set; }
}
