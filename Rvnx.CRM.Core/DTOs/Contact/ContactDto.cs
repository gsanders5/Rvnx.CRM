using Rvnx.CRM.Core.DTOs.Common;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactDto : BaseDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string? JobTitle { get; set; }
        public bool IsHidden { get; set; }
        public Guid? ProfileImageId { get; set; }
        public Guid? LinkedUserId { get; set; }
    }
}
