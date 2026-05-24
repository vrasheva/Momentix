namespace Momentix.Data.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ThemeColor { get; set; } = "#111111";
    public bool IsAdmin { get; set; }
}
