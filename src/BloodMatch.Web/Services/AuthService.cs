using System.Text.Json;
using BloodMatch.Web.Configuration;
using BloodMatch.Web.Models;
using Microsoft.Extensions.Options;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;

namespace BloodMatch.Web.Services;

/// <summary>
/// Wraps Supabase GoTrue operations for registration, login, logout,
/// email verification, and password recovery.
/// </summary>
public sealed class AuthService(
    Supabase.Client supabase,
    SupabaseAuthenticationStateProvider authStateProvider,
    IOptions<SupabaseSettings> settings,
    ILogger<AuthService> logger)
{
    private readonly SupabaseSettings _settings = settings.Value;

    public async Task<AuthResult> RegisterAsync(RegisterInput input, string confirmRedirectUrl)
    {
        if (!_settings.HasClientConfig)
        {
            return new AuthResult(false, ErrorMessage: "Supabase is not configured yet.");
        }

        if (input.Role is not (UserRole.Requester or UserRole.Donor))
        {
            return new AuthResult(false, ErrorMessage: "Only Requester and Donor roles can self-register.");
        }

        try
        {
            var options = new SignUpOptions
            {
                RedirectTo = confirmRedirectUrl,
                Data = new Dictionary<string, object>
                {
                    ["role"] = input.Role == UserRole.Donor ? "donor" : "requester",
                    ["full_name"] = input.FullName.Trim(),
                    ["phone"] = input.Phone?.Trim() ?? string.Empty,
                    ["city"] = input.City.Trim(),
                    ["province"] = input.Province.Trim()
                }
            };

            var session = await supabase.Auth.SignUp(input.Email.Trim(), input.Password, options);

            if (session is not null && !string.IsNullOrWhiteSpace(session.AccessToken))
            {
                var role = await authStateProvider.MarkAuthenticatedAsync(session);
                return new AuthResult(true, Role: role, RequiresEmailConfirmation: false);
            }

            return new AuthResult(true, Role: input.Role, RequiresEmailConfirmation: true);
        }
        catch (GotrueException ex)
        {
            logger.LogWarning(ex, "Registration failed for {Email}.", input.Email);
            return new AuthResult(false, ErrorMessage: ExtractMessage(ex));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected registration error.");
            return new AuthResult(false, ErrorMessage: "Unable to create your account right now. Please try again.");
        }
    }

    public async Task<AuthResult> LoginAsync(LoginInput input)
    {
        if (!_settings.HasClientConfig)
        {
            return new AuthResult(false, ErrorMessage: "Supabase is not configured yet.");
        }

        try
        {
            var session = await supabase.Auth.SignIn(input.Email.Trim(), input.Password);
            if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
            {
                return new AuthResult(false, ErrorMessage: "Invalid email or password.");
            }

            var role = await authStateProvider.MarkAuthenticatedAsync(session);
            return new AuthResult(true, Role: role);
        }
        catch (GotrueException ex)
        {
            logger.LogWarning(ex, "Login failed for {Email}.", input.Email);
            return new AuthResult(false, ErrorMessage: ExtractMessage(ex));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected login error.");
            return new AuthResult(false, ErrorMessage: "Unable to sign in right now. Please try again.");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            if (supabase.Auth.CurrentSession is not null)
            {
                await supabase.Auth.SignOut();
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Supabase remote signout failed; clearing local session anyway.");
        }
        finally
        {
            await authStateProvider.MarkLoggedOutAsync();
        }
    }

    public async Task<AuthResult> SendPasswordResetAsync(string email, string redirectUrl)
    {
        if (!_settings.HasClientConfig)
        {
            return new AuthResult(false, ErrorMessage: "Supabase is not configured yet.");
        }

        try
        {
            await supabase.Auth.ResetPasswordForEmail(new ResetPasswordForEmailOptions(email.Trim())
            {
                RedirectTo = redirectUrl
            });

            return new AuthResult(true);
        }
        catch (GotrueException ex)
        {
            logger.LogWarning(ex, "Password reset request failed for {Email}.", email);
            return new AuthResult(false, ErrorMessage: ExtractMessage(ex));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected password reset error.");
            return new AuthResult(false, ErrorMessage: "Unable to send password reset email right now.");
        }
    }

    public async Task<AuthResult> VerifyTokenHashAsync(string tokenHash, string? type)
    {
        if (!_settings.HasClientConfig)
        {
            return new AuthResult(false, ErrorMessage: "Supabase is not configured yet.");
        }

        var otpType = type?.Trim().ToLowerInvariant() switch
        {
            "recovery" => Constants.EmailOtpType.Recovery,
            "magiclink" => Constants.EmailOtpType.MagicLink,
            "email_change" => Constants.EmailOtpType.EmailChange,
            "invite" => Constants.EmailOtpType.Invite,
            "email" => Constants.EmailOtpType.Email,
            _ => Constants.EmailOtpType.Signup
        };

        try
        {
            var session = await supabase.Auth.VerifyTokenHash(tokenHash, otpType);
            if (session is not null && !string.IsNullOrWhiteSpace(session.AccessToken))
            {
                var role = await authStateProvider.MarkAuthenticatedAsync(session);
                return new AuthResult(true, Role: role);
            }

            return new AuthResult(false, ErrorMessage: "Verification link is invalid or has expired.");
        }
        catch (GotrueException ex)
        {
            logger.LogWarning(ex, "Email verification failed.");
            return new AuthResult(false, ErrorMessage: ExtractMessage(ex));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected token verification error.");
            return new AuthResult(false, ErrorMessage: "Unable to verify your email link.");
        }
    }

    public async Task<AuthResult> UpdatePasswordAsync(string newPassword)
    {
        try
        {
            var user = await supabase.Auth.Update(new UserAttributes { Password = newPassword });
            if (user is null)
            {
                return new AuthResult(false, ErrorMessage: "Password could not be updated.");
            }

            return new AuthResult(true);
        }
        catch (GotrueException ex)
        {
            logger.LogWarning(ex, "Password update failed.");
            return new AuthResult(false, ErrorMessage: ExtractMessage(ex));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected password update error.");
            return new AuthResult(false, ErrorMessage: "Unable to update your password right now.");
        }
    }

    private static string ExtractMessage(GotrueException ex)
    {
        if (string.IsNullOrWhiteSpace(ex.Message))
        {
            return "Authentication request failed.";
        }

        try
        {
            using var doc = JsonDocument.Parse(ex.Message);
            if (doc.RootElement.TryGetProperty("msg", out var msg) && !string.IsNullOrWhiteSpace(msg.GetString()))
            {
                return msg.GetString()!;
            }

            if (doc.RootElement.TryGetProperty("error_description", out var desc) && !string.IsNullOrWhiteSpace(desc.GetString()))
            {
                return desc.GetString()!;
            }

            if (doc.RootElement.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()))
            {
                return message.GetString()!;
            }
        }
        catch (JsonException)
        {
            // Message is already plain text.
        }

        return ex.Message;
    }
}
