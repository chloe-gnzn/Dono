using System.ComponentModel.DataAnnotations;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BloodMatch.Web.Models;

[Table("profiles")]
public sealed class UserProfile : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = string.Empty;

    [Column("role")]
    public string Role { get; set; } = "requester";

    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("province")]
    public string? Province { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    public UserRole ToUserRole() => Role?.Trim().ToLowerInvariant() switch
    {
        "donor" => UserRole.Donor,
        "moderator" => UserRole.Moderator,
        "admin" => UserRole.Admin,
        "requester" => UserRole.Requester,
        _ => UserRole.Requester
    };
}

public sealed record StoredSupabaseSession(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc);

public sealed class RegisterInput : IValidatableObject
{
    [Required(ErrorMessage = "Please choose how you will use BloodMatch.")]
    public UserRole Role { get; set; } = UserRole.Requester;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Enter at least 2 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "City is required.")]
    [StringLength(80)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Province is required.")]
    [StringLength(80)]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role is not (UserRole.Requester or UserRole.Donor))
        {
            yield return new ValidationResult(
                "Self-registration is limited to Requester or Donor accounts.",
                [nameof(Role)]);
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Passwords do not match.",
                [nameof(ConfirmPassword)]);
        }
    }
}

public sealed class LoginInput
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public sealed class ForgotPasswordInput
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordInput : IValidatableObject
{
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            yield return new ValidationResult("Passwords do not match.", [nameof(ConfirmPassword)]);
        }
    }
}

public sealed record AuthResult(
    bool Succeeded,
    UserRole Role = UserRole.Guest,
    bool RequiresEmailConfirmation = false,
    string? ErrorMessage = null);
