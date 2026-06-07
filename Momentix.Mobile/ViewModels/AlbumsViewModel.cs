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
        public ObservableCollection<AlbumResponseDto> MyAlbumsRest { get; } = new();
        public ObservableCollection<AlbumResponseDto> SharedWithMe { get; } = new();
        public ObservableCollection<AlbumResponseDto> SharedWithMeRest { get; } = new();
        public ObservableCollection<AlbumResponseDto> SharedByMe { get; } = new();
        public ObservableCollection<AlbumResponseDto> SharedByMeRest { get; } = new();

        public bool HasMyAlbum0 => MyAlbums.Count > 0;
        public AlbumResponseDto? MyAlbum0 => MyAlbums.Count > 0 ? MyAlbums[0] : null;

        public bool HasSharedWithMe0 => SharedWithMe.Count > 0;
        public AlbumResponseDto? SharedWithMe0 => SharedWithMe.Count > 0 ? SharedWithMe[0] : null;

        public bool HasSharedByMe0 => SharedByMe.Count > 0;
        public AlbumResponseDto? SharedByMe0 => SharedByMe.Count > 0 ? SharedByMe[0] : null;

        public bool SharedWithMeEmpty => SharedWithMe.Count == 0;
        public bool SharedWithMeNotEmpty => SharedWithMe.Count > 0;
        public bool SharedByMeEmpty => SharedByMe.Count == 0;
        public bool SharedByMeNotEmpty => SharedByMe.Count > 0;

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
        public IRelayCommand GoToProfileCommand => new AsyncRelayCommand(async () =>
            await Shell.Current.GoToAsync("ProfilePage"));
        public IRelayCommand GoToNotificationsCommand => new AsyncRelayCommand(async () =>
            await Shell.Current.GoToAsync("NotificationsPage"));

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
                MyAlbumsRest.Clear();
                SharedWithMe.Clear();
                SharedWithMeRest.Clear();
                SharedByMe.Clear();
                SharedByMeRest.Clear();

                if (result != null)
                {
                    foreach (var album in result)
                    {
                        if (album.IsOwner && album.MemberCount == 0)
                            MyAlbums.Add(album);
                        if (album.IsOwner && album.MemberCount > 0)
                            SharedByMe.Add(album);
                        if (album.IsSharedWithMe)
                            SharedWithMe.Add(album);
                    }

                    foreach (var a in MyAlbums.Skip(1))
                        MyAlbumsRest.Add(a);
                    foreach (var a in SharedWithMe.Skip(1))
                        SharedWithMeRest.Add(a);
                    foreach (var a in SharedByMe.Skip(1))
                        SharedByMeRest.Add(a);
                }

                OnPropertyChanged(nameof(HasMyAlbum0));
                OnPropertyChanged(nameof(MyAlbum0));
                OnPropertyChanged(nameof(HasSharedWithMe0));
                OnPropertyChanged(nameof(SharedWithMe0));
                OnPropertyChanged(nameof(HasSharedByMe0));
                OnPropertyChanged(nameof(SharedByMe0));
                OnPropertyChanged(nameof(SharedWithMeEmpty));
                OnPropertyChanged(nameof(SharedWithMeNotEmpty));
                OnPropertyChanged(nameof(SharedByMeEmpty));
                OnPropertyChanged(nameof(SharedByMeNotEmpty));
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