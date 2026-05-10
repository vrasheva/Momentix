using Microsoft.Maui.Controls;

namespace Momentix.Mobile.Services;

public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    public static readonly Dictionary<string, Dictionary<string, Color>> Themes = new()
    {
        ["Purple"] = new()
        {
            ["Primary"] = Color.FromArgb("#7F77DD"),
            ["PrimaryDark"] = Color.FromArgb("#534AB7"),
            ["PrimaryLight"] = Color.FromArgb("#EEEDFE"),
            ["PrimaryBorder"] = Color.FromArgb("#CECBF6"),
            ["PrimaryText"] = Color.FromArgb("#26215C"),
        },
        ["Teal"] = new()
        {
            ["Primary"] = Color.FromArgb("#1D9E75"),
            ["PrimaryDark"] = Color.FromArgb("#0F6E56"),
            ["PrimaryLight"] = Color.FromArgb("#E1F5EE"),
            ["PrimaryBorder"] = Color.FromArgb("#9FE1CB"),
            ["PrimaryText"] = Color.FromArgb("#04342C"),
        },
        ["Coral"] = new()
        {
            ["Primary"] = Color.FromArgb("#D85A30"),
            ["PrimaryDark"] = Color.FromArgb("#993C1D"),
            ["PrimaryLight"] = Color.FromArgb("#FAECE7"),
            ["PrimaryBorder"] = Color.FromArgb("#F5C4B3"),
            ["PrimaryText"] = Color.FromArgb("#4A1B0C"),
        },
        ["Pink"] = new()
        {
            ["Primary"] = Color.FromArgb("#D4537E"),
            ["PrimaryDark"] = Color.FromArgb("#993556"),
            ["PrimaryLight"] = Color.FromArgb("#FBEAF0"),
            ["PrimaryBorder"] = Color.FromArgb("#F4C0D1"),
            ["PrimaryText"] = Color.FromArgb("#4B1528"),
        },
        ["Amber"] = new()
        {
            ["Primary"] = Color.FromArgb("#BA7517"),
            ["PrimaryDark"] = Color.FromArgb("#854F0B"),
            ["PrimaryLight"] = Color.FromArgb("#FAEEDA"),
            ["PrimaryBorder"] = Color.FromArgb("#FAC775"),
            ["PrimaryText"] = Color.FromArgb("#412402"),
        },
    };

    public static readonly Dictionary<string, int> ThemeXpRequired = new()
    {
        ["Purple"] = 0,
        ["Teal"] = 0,
        ["Coral"] = 150,
        ["Pink"] = 300,
        ["Amber"] = 500,
    };

    private string _currentTheme = "Purple";
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
        var name = Preferences.Get("theme_name", "Purple");
        var dark = Preferences.Get("theme_dark", false);
        ApplyTheme(name, dark);
    }
}