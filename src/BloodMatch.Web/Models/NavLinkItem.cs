namespace BloodMatch.Web.Models;

public sealed record NavLinkItem(
    string Text,
    string Href,
    UserRole[]? VisibleTo = null,
    bool ExactMatch = false)
{
    public bool IsVisibleTo(UserRole role) => VisibleTo is null || VisibleTo.Contains(role);
}
