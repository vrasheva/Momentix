using Microsoft.Maui.Controls;

namespace Momentix.Mobile.Services;

public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    public static readonly Dictionary<string, Dictionary<string, Color>> Themes = new()
    {
        ["Blue"] = new()
        {
            ["Primary"] = Color.FromArgb("#60A5FA"),
            ["PrimaryDark"] = Color.FromArgb("#2563EB"),
            ["PrimaryLight"] = Color.FromArgb("#DBEAFE"),
            ["PrimaryBorder"] = Color.FromArgb("#93C5FD"),
            ["PrimaryText"] = Color.FromArgb("#1E3A5F"),
        },
        ["Green"] = new()
        {
            ["Primary"] = Color.FromArgb("#34D399"),
            ["PrimaryDark"] = Color.FromArgb("#059669"),
            ["PrimaryLight"] = Color.FromArgb("#D1FAE5"),
            ["PrimaryBorder"] = Color.FromArgb("#6EE7B7"),
            ["PrimaryText"] = Color.FromArgb("#064E3B"),
        },
        ["Yellow"] = new()
        {
            ["Primary"] = Color.FromArgb("#FBBF24"),
            ["PrimaryDark"] = Color.FromArgb("#D97706"),
            ["PrimaryLight"] = Color.FromArgb("#FEF3C7"),
            ["PrimaryBorder"] = Color.FromArgb("#FCD34D"),
            ["PrimaryText"] = Color.FromArgb("#78350F"),
        },
        ["Purple"] = new()
        {
            ["Primary"] = Color.FromArgb("#A78BFA"),
            ["PrimaryDark"] = Color.FromArgb("#7C3AED"),
            ["PrimaryLight"] = Color.FromArgb("#EDE9FE"),
            ["PrimaryBorder"] = Color.FromArgb("#C4B5FD"),
            ["PrimaryText"] = Color.FromArgb("#2E1065"),
        },
        ["Black"] = new()
        {
            ["Primary"] = Color.FromArgb("#1F2937"),
            ["PrimaryDark"] = Color.FromArgb("#111827"),
            ["PrimaryLight"] = Color.FromArgb("#F3F4F6"),
            ["PrimaryBorder"] = Color.FromArgb("#6B7280"),
            ["PrimaryText"] = Color.FromArgb("#F9FAFB"),
        },
    };

    public static readonly Dictionary<string, int> ThemeXpRequired = new()
    {
        ["Blue"] = 0,
        ["Green"] = 0,
        ["Yellow"] = 0,
        ["Purple"] = 0,
        ["Black"] = 0,
    };

    private string _currentTheme = "Blue";
    private bool _isDark = false;

    public string CurrentTheme => _currentTheme;
    public bool IsDark => _isDark;

    public void ApplyTheme(string themeName, bool dark)
    {
        if (!Themes.ContainsKey(themeName)) return;
        _currentTheme = themeName;
        _isDark = dark;

        if (Application.Current == null) return;
        var dict = Application.Current.Resources;

        var colors = Themes[themeName];
        foreach (var kv in colors)
            dict[kv.Key] = kv.Value;

        if (dark)
        {
            dict["PageBackground"] = Color.FromArgb("#0a0a12");
            dict["CardBackground"] = Color.FromArgb("#1a1a2e");
            dict["TextPrimary"] = Colors.White;
            dict["TextSecondary"] = Color.FromArgb("#AFA9EC");
            dict["BorderColor"] = Color.FromArgb("#FFFFFF18");
            dict["TabBackground"] = Color.FromArgb("#0f0f1a");
        }
        else
        {
            dict["PageBackground"] = Color.FromArgb("#f7f6f2");
            dict["CardBackground"] = Colors.White;
            dict["TextPrimary"] = Color.FromArgb("#1a1a1a");
            dict["TextSecondary"] = Color.FromArgb("#888888");
            dict["BorderColor"] = Color.FromArgb("#E5E3DC");
            dict["TabBackground"] = Colors.White;
        }

        Preferences.Set("theme_name", themeName);
        Preferences.Set("theme_dark", dark);
    }

    public void LoadSaved()
    {
        var name = Preferences.Get("theme_name", "Blue");
        var dark = Preferences.Get("theme_dark", false);
        ApplyTheme(name, dark);
    }
}