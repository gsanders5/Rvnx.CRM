namespace Rvnx.CRM.Core.DTOs.Contact;

public class FavoriteSidebarItemDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public Guid? ProfileImageId { get; set; }
}
