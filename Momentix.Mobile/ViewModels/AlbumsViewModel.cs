using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels
{
    public partial class AlbumsViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        public IRelayCommand CreateAlbumCommand => new AsyncRelayCommand(CreateAlbum);
        public IRelayCommand GoToCreateAlbumCommand => new AsyncRelayCommand(GoToCreateAlbum);

        public ObservableCollection<AlbumResponseDto> Albums { get; } = new();

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public IRelayCommand LoadAlbumsCommand => new AsyncRelayCommand(LoadAlbums);
        public IRelayCommand LogoutCommand => new AsyncRelayCommand(Logout);
        public IRelayCommand<AlbumResponseDto> OpenAlbumCommand => new AsyncRelayCommand<AlbumResponseDto>(OpenAlbum);

        public AlbumsViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        private async Task LoadAlbums()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _apiService.GetAsync<List<AlbumResponseDto>>("Albums");

                Albums.Clear();

                if (result != null)
                {
                    foreach (var album in result)
                        Albums.Add(album);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task Logout()
        {
            Preferences.Remove("auth_token");
            Preferences.Remove("user_name");
            Preferences.Remove("user_id");
            _apiService.ClearToken();

            await Shell.Current.GoToAsync("//LoginPage");
        }
        private async Task CreateAlbum()
        {
            try
            {
                var newAlbum = await _apiService.PostAsync<CreateAlbumDto, AlbumResponseDto>(
                    "Albums",
                    new CreateAlbumDto
                    {
                        Title = "New Album",
                        Description = "Created from mobile"
                    });

                if (newAlbum != null)
                {
                    Albums.Insert(0, newAlbum);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
        private async Task GoToCreateAlbum()
        {
            try
            {
                await Shell.Current.GoToAsync("CreateAlbumPage");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private async Task OpenAlbum(AlbumResponseDto? album)
        {
            if (album == null)
                return;

            var parameters = new Dictionary<string, object>
            {
                ["AlbumId"] = album.Id,
                ["AlbumTitle"] = album.Title
            };

            try
            {
                await Shell.Current.GoToAsync("AlbumDetailsPage", parameters);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}
