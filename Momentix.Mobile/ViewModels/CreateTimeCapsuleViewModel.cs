using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

public partial class CreateTimeCapsuleViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<SelectedPhotoItemViewModel> SelectedPhotos { get; } = new();
    public ObservableCollection<FriendInviteViewModel> Friends { get; } = new();

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    private DateTime _unlockDate = DateTime.Today.AddDays(1);
    public DateTime UnlockDate
    {
        get => _unlockDate;
        set { _unlockDate = value; OnPropertyChanged(); }
    }

    private TimeSpan _unlockTime = DateTime.Now.AddHours(1).TimeOfDay;
    public TimeSpan UnlockTime
    {
        get => _unlockTime;
        set { _unlockTime = value; OnPropertyChanged(); }
    }

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

    public IRelayCommand SaveCommand => new AsyncRelayCommand(Save);
    public IRelayCommand LoadFriendsCommand => new AsyncRelayCommand(LoadFriends);
    public IRelayCommand PickPhotoCommand => new AsyncRelayCommand(PickPhoto);
    public IRelayCommand<SelectedPhotoItemViewModel> RemovePhotoCommand => new RelayCommand<SelectedPhotoItemViewModel>(RemovePhoto);
    public IRelayCommand CancelCommand => new AsyncRelayCommand(Cancel);

    public string SelectedPhotoCountText => SelectedPhotos.Count == 0
        ? "Няма избрани снимки"
        : $"{SelectedPhotos.Count} снимки избрани";

    public CreateTimeCapsuleViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void OnAppearing()
    {
        LoadFriendsCommand.Execute(null);
    }

    private async Task LoadFriends()
    {
        try
        {
            var invitedIds = Friends
                .Where(f => f.IsInvited)
                .Select(f => f.UserId)
                .ToHashSet();

            var result = await _apiService.GetAsync<List<FriendResponseDto>>("Friends");
            Friends.Clear();

            if (result == null) return;

            foreach (var friend in result
                .GroupBy(f => f.UserId)
                .Select(g => g.First())
                .OrderBy(f => f.FullName))
            {
                Friends.Add(new FriendInviteViewModel(friend)
                {
                    IsInvited = invitedIds.Contains(friend.UserId)
                });
            }
        }
        catch { }
    }

    private async Task PickPhoto()
    {
        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Избери снимка"
            });

            if (file == null) return;

            SelectedPhotos.Add(new SelectedPhotoItemViewModel(file));
            OnPropertyChanged(nameof(SelectedPhotoCountText));
            ErrorMessage = string.Empty;
        }
        catch (FeatureNotSupportedException) { ErrorMessage = "Снимките не се поддържат на това устройство."; }
        catch (PermissionException) { ErrorMessage = "Нямаш разрешение за снимки."; }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    private void RemovePhoto(SelectedPhotoItemViewModel? photo)
    {
        if (photo == null) return;
        SelectedPhotos.Remove(photo);
        OnPropertyChanged(nameof(SelectedPhotoCountText));
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Заглавието е задължително.";
            return;
        }

        var localUnlockAt = UnlockDate.Date.Add(UnlockTime);
        if (localUnlockAt <= DateTime.Now)
        {
            ErrorMessage = "Датата на отключване трябва да е в бъдещето.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _apiService.PostAsync<CreateTimeCapsuleDto, TimeCapsuleResponseDto>(
                "TimeCapsule",
                new CreateTimeCapsuleDto
                {
                    Title = Title,
                    Description = Description,
                    UnlockAt = localUnlockAt.ToUniversalTime()
                });

            if (result == null)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Капсулата не беше създадена."
                    : _apiService.LastErrorMessage;
                return;
            }

            foreach (var friend in Friends.Where(f => f.IsInvited))
            {
                var success = await _apiService.PostAsync(
                    $"TimeCapsule/{result.Id}/members",
                    new AddAlbumMemberDto { UserEmail = friend.Email, CanUpload = false });

                if (!success)
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                        ? "Капсулата е създадена, но приятелят не беше добавен."
                        : _apiService.LastErrorMessage;
                    return;
                }
            }

            foreach (var photo in SelectedPhotos.ToList())
            {
                await using var stream = await photo.File.OpenReadAsync();
                var contentType = string.IsNullOrWhiteSpace(photo.File.ContentType)
                    ? "image/jpeg" : photo.File.ContentType;

                var uploadResult = await _apiService.PostFileAsync<MediaResponseDto>(
                    $"Media/timecapsule/{result.Id}/photo",
                    stream, photo.File.FileName, contentType);

                if (uploadResult == null)
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                        ? "Капсулата е създадена, но снимката не беше качена."
                        : _apiService.LastErrorMessage;
                    return;
                }
            }

            Title = string.Empty;
            Description = string.Empty;
            SelectedPhotos.Clear();
            foreach (var friend in Friends)
                friend.IsInvited = false;

            OnPropertyChanged(nameof(SelectedPhotoCountText));
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }

    public class FriendInviteViewModel : BaseViewModel
    {
        private readonly FriendResponseDto _friend;

        public string UserId => _friend.UserId;
        public string FullName => _friend.FullName;
        public string Email => _friend.Email;

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName)) return "?";
                var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
                return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
            }
        }

        public string AvatarColor
        {
            get
            {
                var theme = Preferences.Get("theme_name", "Blue");
                return theme switch
                {
                    "Blue" => "#60A5FA",
                    "Green" => "#34D399",
                    "Yellow" => "#FBBF24",
                    "Purple" => "#A78BFA",
                    "Black" => "#1F2937",
                    _ => "#60A5FA"
                };
            }
        }

        private bool _isInvited;
        public bool IsInvited
        {
            get => _isInvited;
            set { _isInvited = value; OnPropertyChanged(); }
        }

        public FriendInviteViewModel(FriendResponseDto friend)
        {
            _friend = friend;
        }
    }
}