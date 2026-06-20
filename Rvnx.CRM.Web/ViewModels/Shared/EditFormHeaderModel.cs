[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The namespace intentionally mirrors the conventional Views/Shared folder; VB consumers are not a concern for this web app.", Scope = "namespace", Target = "~N:Rvnx.CRM.Web.ViewModels.Shared")]

// CA1716: "Shared" is a VB reserved keyword, but the namespace intentionally mirrors
// the conventional Views/Shared folder; VB consumers are not a concern for this web app.
namespace Rvnx.CRM.Web.ViewModels.Shared;

/// <summary>
/// Drives Views/Shared/_EditFormHeader.cshtml: the action strip (back link,
/// unsaved indicator, submit button) plus the editorial masthead shared by
/// the entity Create/Edit pages. Must be rendered inside the page's
/// &lt;form data-edit-form&gt; so the submit button and indicator belong to it.
/// </summary>
public class EditFormHeaderModel
{
    public required string BackUrl { get; init; }
    public string BackText { get; init; } = "Back to Contact";
    public string SaveText { get; init; } = "Save";
    public string SaveIcon { get; init; } = "bi-save";

    /// <summary>Bootstrap icon class for the masthead placeholder, e.g. "bi-journal-text".</summary>
    public required string Icon { get; init; }
    public required string Eyebrow { get; init; }
    public required string Title { get; init; }
}
