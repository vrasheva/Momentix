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
                    fonts.AddFont("MaterialIconsOutlined-Regular.otf", "MaterialIcons"); 
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
            builder.Services.AddTransient<FriendsViewModel>();
            builder.Services.AddTransient<FriendsPage>();
            builder.Services.AddTransient<TimeCapsulesViewModel>();
            builder.Services.AddTransient<TimeCapsulesPage>();
            builder.Services.AddTransient<CreateTimeCapsuleViewModel>();
            builder.Services.AddTransient<CreateTimeCapsulePage>();
            builder.Services.AddTransient<TimeCapsuleDetailsViewModel>();
            builder.Services.AddTransient<TimeCapsuleDetailsPage>();
            builder.Services.AddTransient<ChallengesViewModel>();
            builder.Services.AddTransient<ChallengesPage>();
            builder.Services.AddTransient<NotificationsViewModel>();
            builder.Services.AddTransient<NotificationsPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}

