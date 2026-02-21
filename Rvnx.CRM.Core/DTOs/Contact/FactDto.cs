using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Base;

namespace Rvnx.CRM.Core.DTOs.Contact
{
    public class FactDto : BaseDto
    {
        public string Category { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
    }
}
