// Assembly-level suppression is in EditFormHeaderModel.cs

namespace Rvnx.CRM.Web.ViewModels.Shared;

/// <summary>
/// Drives Views/Shared/_SaveShortcutCard.cshtml: the side-column card with a
/// save button (linked to the main form via the HTML5 form attribute) and a
/// cancel link, shared by the entity Create/Edit pages.
/// </summary>
public class SaveShortcutCardModel
{
    /// <summary>Id of the &lt;form&gt; the save button submits, e.g. "note-form".</summary>
    public required string FormId { get; init; }

    public required string CancelUrl { get; init; }
    public string CancelText { get; init; } = "Cancel";
    public string CancelIcon { get; init; } = "bi-arrow-left";
    public string SaveText { get; init; } = "Save";
    public string SaveIcon { get; init; } = "bi-save";
    public string Title { get; init; } = "Ready to save?";
    public string Description { get; init; } = "Changes are applied immediately.";
}
