namespace BloodMatch.Web.Models;

/// <summary>
/// Who is using the app. <see cref="Guest"/> means not signed in.
/// Real roles will come from the profiles table once auth is added.
/// </summary>
public enum UserRole
{
    Guest,
    Requester,
    Donor,
    Moderator,
    Admin
}
