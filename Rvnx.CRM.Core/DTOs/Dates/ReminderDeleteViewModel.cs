using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Dates;

namespace Rvnx.CRM.Core.DTOs.Dates
{
    public class ReminderDeleteViewModel : ReminderDto
    {
        public string EntityName { get; set; } = string.Empty;
    }
}
