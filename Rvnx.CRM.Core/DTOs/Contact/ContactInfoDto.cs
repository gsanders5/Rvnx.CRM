using Rvnx.CRM.Core.DTOs.Common;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactInfoDto : BaseDto
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Label { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
    }
}
