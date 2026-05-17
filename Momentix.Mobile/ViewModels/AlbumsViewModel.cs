using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels
{
    public partial class AlbumsViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;

        public ObservableCollection<AlbumResponseDto> MyAlbums { get; } = new();
        public ObservableCollection<AlbumResponseDto> SharedWithMe { get; } = new();
        public ObservableCollection<AlbumResponseDto> SharedByMe { get; } = new();

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

        public string UserName => Preferences.Get("user_name", "");

        public IRelayCommand LoadAlbumsCommand => new AsyncRelayCommand(LoadAlbums);
        public IRelayCommand LogoutCommand => new AsyncRelayCommand(Logout);
        public IRelayCommand GoToCreateAlbumCommand => new AsyncRelayCommand(GoToCreateAlbum);
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

                MyAlbums.Clear();
                SharedWithMe.Clear();
                SharedByMe.Clear();

                if (result != null)
                {
                    foreach (var album in result)
                    {
                        if (album.IsOwner && album.MemberCount == 0)
                            MyAlbums.Add(album);
                        else if (album.IsOwner && album.MemberCount > 0)
                        {
                            MyAlbums.Add(album);
                            SharedByMe.Add(album);
                        }
                        else if (album.IsSharedWithMe)
                            SharedWithMe.Add(album);
                    }
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

        private async Task GoToCreateAlbum()
        {
            await Shell.Current.GoToAsync("CreateAlbumPage");
        }

        private async Task OpenAlbum(AlbumResponseDto? album)
        {
            if (album == null) return;
            var parameters = new Dictionary<string, object>
            {
                ["AlbumId"] = album.Id,
                ["AlbumTitle"] = album.Title
            };
            await Shell.Current.GoToAsync("AlbumDetailsPage", parameters);
        }
    }
}