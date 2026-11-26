namespace BloodMatch.Web.Models;

/// <summary>
/// A navigation entry. <paramref name="VisibleTo"/> = null means visible to everyone.
/// </summary>
public sealed record NavLinkItem(
    string Text,
    string Href,
    UserRole[]? VisibleTo = null,
    bool ExactMatch = false)
{
    public bool IsVisibleTo(UserRole role) => VisibleTo is null || VisibleTo.Contains(role);
}
