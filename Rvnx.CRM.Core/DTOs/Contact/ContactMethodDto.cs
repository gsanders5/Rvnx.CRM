using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Enumerations;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactMethodDto : BaseDto
    {
        public ContactMethodType Type { get; set; }
        public string Value { get; set; } = string.Empty;
        public string? Label { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
    }
}
