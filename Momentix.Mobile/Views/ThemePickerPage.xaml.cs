using Microsoft.Maui.Controls.Shapes;
using Momentix.Mobile.Services;

namespace Momentix.Mobile.Views;

public partial class ThemePickerPage : ContentPage
{
    private string _selected = "Purple";
    private int _userXp = 0;

    public ThemePickerPage()
    {
        InitializeComponent();
        _userXp = Preferences.Get("user_xp", 0);
        BuildSwatches();
    }

    private void BuildSwatches()
    {
        SwatchContainer.Children.Clear();
        var colors = new (string name, string hex)[]
        {
            ("Purple","#7F77DD"),("Teal","#1D9E75"),
            ("Coral","#D85A30"),("Pink","#D4537E"),("Amber","#BA7517")
        };

        foreach (var (name, hex) in colors)
        {
            int required = ThemeService.ThemeXpRequired[name];
            bool locked = _userXp < required;

            var border = new Border
            {
                WidthRequest = 44,
                HeightRequest = 44,
                BackgroundColor = Color.FromArgb(hex),
                Opacity = locked ? 0.35 : 1.0,
                Padding = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 22 },
                Stroke = name == _selected ? Colors.Black : Colors.Transparent
            };

            if (!locked)
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += (s, e) => SelectTheme(name);
                border.GestureRecognizers.Add(tap);
            }
            else
            {
                // Показва tooltip с нужните XP
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, e) =>
                    await DisplayAlert("Заключено", $"Нужни са {required} XP", "OK");
                border.GestureRecognizers.Add(tap);
            }

            SwatchContainer.Children.Add(border);
        }
    }

    private void SelectTheme(string name)
    {
        _selected = name;
        ThemeService.Instance.ApplyTheme(name, DarkSwitch.IsToggled);
        BuildSwatches();
    }

    private void OnDarkToggled(object sender, ToggledEventArgs e)
    {
        ThemeService.Instance.ApplyTheme(_selected, e.Value);
    }

    private async void OnSave(object sender, EventArgs e)
    {
        ThemeService.Instance.ApplyTheme(_selected, DarkSwitch.IsToggled);
        await Shell.Current.GoToAsync("//RegisterPage");
    }
}