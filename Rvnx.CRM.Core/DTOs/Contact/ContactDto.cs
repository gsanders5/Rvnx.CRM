using Rvnx.CRM.Core.DTOs.Common;
using System;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class ContactDto : BaseDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? JobTitle { get; set; }
    }
}
