using Rvnx.CRM.Core.DTOs.Business;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactDetailDto : ContactDto
    {
        public string? Nickname { get; set; }

        public IEnumerable<NoteDto> Notes { get; set; } = new List<NoteDto>();
        public IEnumerable<ReminderDto> Reminders { get; set; } = new List<ReminderDto>();
        public IEnumerable<SignificantDateDto> SignificantDates { get; set; } = new List<SignificantDateDto>();
        public IEnumerable<RelationshipDto> Relationships { get; set; } = new List<RelationshipDto>();
        public IEnumerable<RelationshipDto> RelatedTo { get; set; } = new List<RelationshipDto>();
        public IEnumerable<PetDto> Pets { get; set; } = new List<PetDto>();
        public IEnumerable<EmployerDto> Employers { get; set; } = new List<EmployerDto>();
        public IEnumerable<ContactMethodDto> ContactMethods { get; set; } = new List<ContactMethodDto>();
        public IEnumerable<FactDto> Facts { get; set; } = new List<FactDto>();
        public IEnumerable<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
    }
}
