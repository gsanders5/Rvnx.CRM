using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Business;
using Rvnx.CRM.Core.DTOs.Dates;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactDetailDto : ContactDto
    {
        public string? Nickname { get; set; }

        public IEnumerable<NoteDto> Notes { get; set; } = [];
        public IEnumerable<ReminderDto> Reminders { get; set; } = [];
        public IEnumerable<SignificantDateDto> SignificantDates { get; set; } = [];
        public IEnumerable<RelationshipDto> Relationships { get; set; } = [];
        public IEnumerable<RelationshipDto> RelatedTo { get; set; } = [];
        public IEnumerable<PetDto> Pets { get; set; } = [];
        public IEnumerable<EmployerDto> Employers { get; set; } = [];
        public IEnumerable<ContactMethodDto> ContactMethods { get; set; } = [];
        public IEnumerable<FactDto> Facts { get; set; } = [];
        public IEnumerable<AttachmentDto> Attachments { get; set; } = [];
        public IEnumerable<InteractionDto> Timeline { get; set; } = [];
    }
}
