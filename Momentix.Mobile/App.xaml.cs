using Momentix.Mobile.Services;

namespace Momentix.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ThemeService.Instance.LoadSaved();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
