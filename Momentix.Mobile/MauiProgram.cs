using Microsoft.Extensions.Logging;
using Momentix.Mobile.Services;
using Momentix.Mobile.ViewModels;
using Momentix.Mobile.Views;

namespace Momentix.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<ApiService>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<AlbumsViewModel>();
            builder.Services.AddTransient<AlbumsPage>();
            builder.Services.AddTransient<AlbumDetailsViewModel>();
            builder.Services.AddTransient<AlbumDetailsPage>();
            builder.Services.AddTransient<CreateAlbumViewModel>();
            builder.Services.AddTransient<CreateAlbumPage>();
            builder.Services.AddTransient<TimeCapsulesViewModel>();
            builder.Services.AddTransient<TimeCapsulesPage>();
            builder.Services.AddTransient<CreateTimeCapsuleViewModel>();
            builder.Services.AddTransient<CreateTimeCapsulePage>();
            builder.Services.AddTransient<ChallengesViewModel>();
            builder.Services.AddTransient<ChallengesPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
