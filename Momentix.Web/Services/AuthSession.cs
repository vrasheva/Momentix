using Momentix.Data.DTOs;

namespace Momentix.Web.Services;

public class AuthSession
{
    public string? Token { get; private set; }
    public string? UserId { get; private set; }
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string ThemeColor { get; private set; } = "#111111";

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

    public event Action? Changed;

    public void SignIn(AuthResponseDto response)
    {
        Token = response.Token;
        UserId = response.UserId;
        FullName = response.FullName;
        Email = response.Email;
        ThemeColor = string.IsNullOrWhiteSpace(response.ThemeColor) ? "#111111" : response.ThemeColor;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        Token = null;
        UserId = null;
        FullName = null;
        Email = null;
        ThemeColor = "#111111";
        Changed?.Invoke();
    }
}
