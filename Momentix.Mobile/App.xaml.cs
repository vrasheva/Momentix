using Momentix.Mobile.Services;

namespace Momentix.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
            ThemeService.Instance.LoadSaved();
        }
    }
}