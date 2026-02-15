using Rvnx.CRM.Core.DTOs.Business;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Pet;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactDetailDto : ContactDto
    {
        public string? Nickname { get; set; }

        public IEnumerable<NoteDto> Notes { get; set; } = new List<NoteDto>();
        public IEnumerable<ReminderDto> Reminders { get; set; } = new List<ReminderDto>();
        public IEnumerable<ImportantDateDto> ImportantDates { get; set; } = new List<ImportantDateDto>();
        public IEnumerable<RelationshipDto> Relationships { get; set; } = new List<RelationshipDto>();
        public IEnumerable<RelationshipDto> RelatedTo { get; set; } = new List<RelationshipDto>();
        public IEnumerable<PetDto> Pets { get; set; } = new List<PetDto>();
        public IEnumerable<EmployerDto> Employers { get; set; } = new List<EmployerDto>();
        public IEnumerable<ContactInfoDto> ContactInfos { get; set; } = new List<ContactInfoDto>();
        public IEnumerable<FactDto> Facts { get; set; } = new List<FactDto>();
        public IEnumerable<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
    }
}
