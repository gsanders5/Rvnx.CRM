namespace Rvnx.CRM.Core.Interfaces
{
    public interface IRemindableDto
    {
        bool RemindMe { get; set; }
        TimeSpan EventFrequency { get; set; }
    }
}
