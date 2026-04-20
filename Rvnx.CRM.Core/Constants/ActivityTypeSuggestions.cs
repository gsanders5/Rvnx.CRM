namespace Rvnx.CRM.Core.Constants;

public static class ActivityTypeSuggestions
{
    public static readonly IReadOnlyList<string> All =
    [
        "Phone Call",
        "Video Call",
        "Meeting",
        "Dinner",
        "Lunch",
        "Coffee",
        "Drinks",
        "Concert",
        "Movie",
        "Travel",
        "Event",
        "Text/Message",
        "Email"
    ];

    public record QuickLogOption(string Type, string Icon, string Label);

    public static readonly IReadOnlyList<QuickLogOption> QuickLog =
    [
        new("Phone Call", "fa-solid fa-phone", "Call"),
        new("Meeting", "fa-solid fa-handshake", "Met"),
        new("Text/Message", "fa-solid fa-comment-dots", "Messaged")
    ];
}