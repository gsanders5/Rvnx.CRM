namespace Rvnx.CRM.Core.DTOs.Base;

public class BaseDto
{
    public Guid Id { get; set; }
    public string CreatedBy { get; set; } = "System";
    public string LastChangedBy { get; set; } = "System";
    public DateTime CreatedDate { get; set; }
    public DateTime LastChangedDate { get; set; }
    public string? UserId { get; set; }
    public string? GroupId { get; set; }
}
