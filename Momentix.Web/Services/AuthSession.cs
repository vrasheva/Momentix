using Momentix.Data.DTOs;

namespace Momentix.Web.Services;

public class AuthSession
{
    private static readonly Dictionary<string, string> ThemeHexMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Blue"]   = "#60A5FA",
        ["Purple"] = "#A78BFA",
        ["Green"]  = "#34D399",
        ["Yellow"] = "#FBBF24",
        ["Black"]  = "#1F2937",
    };

    public string? Token { get; private set; }
    public string? UserId { get; private set; }
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string ThemeColor { get; private set; } = "Black";
    public string ThemeHex => ThemeHexMap.TryGetValue(ThemeColor, out var h) ? h
                            : ThemeColor.StartsWith('#') ? ThemeColor : "#1F2937";
    public bool IsAdmin { get; private set; }

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

    public event Action? Changed;

    public void SignIn(AuthResponseDto response)
    {
        Token = response.Token;
        UserId = response.UserId;
        FullName = response.FullName;
        Email = response.Email;
        ThemeColor = string.IsNullOrWhiteSpace(response.ThemeColor) ? "Black" : response.ThemeColor;
        IsAdmin = response.IsAdmin;
        Changed?.Invoke();
    }

    public void UpdateProfile(string fullName, string email)
    {
        FullName = fullName;
        Email = email;
        Changed?.Invoke();
    }

    public void UpdateTheme(string themeColor)
    {
        ThemeColor = themeColor;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        Token = null;
        UserId = null;
        FullName = null;
        Email = null;
        ThemeColor = "Black";
        IsAdmin = false;
        Changed?.Invoke();
    }
}
