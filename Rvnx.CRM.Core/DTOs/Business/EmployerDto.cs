using Rvnx.CRM.Core.DTOs.Common;

namespace Rvnx.CRM.Core.DTOs.Business
{
    public class EmployerDto : BaseDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string? JobTitle { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid EmployeeId { get; set; }
    }
}
