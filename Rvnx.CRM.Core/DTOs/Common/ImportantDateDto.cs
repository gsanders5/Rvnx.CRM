using System;

namespace Rvnx.CRM.Core.DTOs.Common
{
    public class ImportantDateDto : BaseDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Description { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
    }
}
