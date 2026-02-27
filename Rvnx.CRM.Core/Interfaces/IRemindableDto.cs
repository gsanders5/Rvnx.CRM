namespace Rvnx.CRM.Core.Interfaces
{
    public interface IRemindableDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether a reminder should be active for this item.
        /// </summary>
        bool RemindMe { get; set; }

        /// <summary>
        /// Gets or sets the frequency of the reminder (e.g., daily, yearly).
        /// </summary>
        TimeSpan EventFrequency { get; set; }
    }
}
