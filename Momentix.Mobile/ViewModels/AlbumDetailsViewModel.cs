using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Data.Models;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

[QueryProperty(nameof(AlbumId), nameof(AlbumId))]
[QueryProperty(nameof(AlbumTitle), nameof(AlbumTitle))]
public partial class AlbumDetailsViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private bool _isPageVisible;

    public ObservableCollection<AlbumMediaItemViewModel> MediaItems { get; } = new();
    public ObservableCollection<AlbumMemberResponseDto> Members { get; } = new();
    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();
    public ObservableCollection<AlbumMediaItemViewModel> PhotoItems { get; } = new();
    public ObservableCollection<AlbumMediaItemViewModel> RestPhotos { get; } = new();
    public bool HasPhotos => PhotoItems.Count > 0;
    public AlbumMediaItemViewModel? FirstPhoto => PhotoItems.Count > 0 ? PhotoItems[0] : null;

    private int _albumId;
    public int AlbumId
    {
        get => _albumId;
        set
        {
            if (_albumId == value) return;
            _albumId = value;
            OnPropertyChanged();
            LoadIfReady();
        }
    }

    private string _albumTitle = string.Empty;
    public string AlbumTitle
    {
        get => _albumTitle;
        set { _albumTitle = value; OnPropertyChanged(); }
    }

    private string _memberEmail = string.Empty;
    public string MemberEmail
    {
        get => _memberEmail;
        set { _memberEmail = value; OnPropertyChanged(); }
    }

    private FriendItemViewModel? _selectedFriend;
    public FriendItemViewModel? SelectedFriend
    {
        get => _selectedFriend;
        set
        {
            _selectedFriend = value;
            OnPropertyChanged();

            if (value != null)
                MemberEmail = value.Email;
        }
    }

    private string _letterText = string.Empty;
    public string LetterText
    {
        get => _letterText;
        set { _letterText = value; OnPropertyChanged(); }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public IRelayCommand LoadCommand => new AsyncRelayCommand(Load);
    public IRelayCommand AddMemberCommand => new AsyncRelayCommand(AddMember);
    public IRelayCommand AddSelectedFriendCommand => new AsyncRelayCommand(AddSelectedFriend);
    public IRelayCommand LoadFriendsCommand => new AsyncRelayCommand(LoadFriends);
    public IRelayCommand AddLetterCommand => new AsyncRelayCommand(AddLetter);
    public IRelayCommand PickPhotoCommand => new AsyncRelayCommand(PickPhoto);
    public IRelayCommand BackCommand => new AsyncRelayCommand(Back);

    public AlbumDetailsViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void PageAppeared()
    {
        _isPageVisible = true;
        LoadIfReady();
    }

    private void LoadIfReady()
    {
        if (!_isPageVisible || AlbumId <= 0 || IsLoading)
            return;
        LoadCommand.Execute(null);
    }

    private async Task Load()
    {
        if (AlbumId <= 0) return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var media = await _apiService.GetAsync<List<MediaResponseDto>>($"Media/album/{AlbumId}");

            MediaItems.Clear();
            PhotoItems.Clear();
            RestPhotos.Clear();

            if (media != null)
            {
                foreach (var item in media)
                    MediaItems.Add(new AlbumMediaItemViewModel(item));

                var photos = media.Where(m => m.Type == MediaType.Image).ToList();
                foreach (var item in photos)
                    PhotoItems.Add(new AlbumMediaItemViewModel(item));

                foreach (var item in photos.Skip(1))
                    RestPhotos.Add(new AlbumMediaItemViewModel(item));

                OnPropertyChanged(nameof(HasPhotos));
                OnPropertyChanged(nameof(FirstPhoto));
            }

            var members = await _apiService.GetAsync<List<AlbumMemberResponseDto>>($"Albums/{AlbumId}/members");
            Members.Clear();
            if (members != null)
                foreach (var m in members)
                    Members.Add(m);

            await LoadFriends();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadFriends()
    {
        var result = await _apiService.GetAsync<List<FriendResponseDto>>("Friends");
        Friends.Clear();

        if (result == null)
            return;

        var existingMemberIds = Members.Select(m => m.UserId).ToHashSet();

        foreach (var friend in result
            .GroupBy(f => f.UserId)
            .Select(g => g.First())
            .Where(f => !existingMemberIds.Contains(f.UserId))
            .OrderBy(f => f.FullName))
            Friends.Add(new FriendItemViewModel(friend));

        if (SelectedFriend != null && !Friends.Any(f => f.UserId == SelectedFriend.UserId))
            SelectedFriend = null;
    }

    private async Task AddSelectedFriend()
    {
        if (SelectedFriend == null)
        {
            StatusMessage = "Ð˜Ð·Ð±ÐµÑ€Ð¸ Ð¿Ñ€Ð¸ÑÑ‚ÐµÐ» Ð¿ÑŠÑ€Ð²Ð¾.";
            return;
        }

        await AddMemberByEmail(SelectedFriend.Email);
    }

    private async Task AddMember()
    {
        await AddMemberByEmail(MemberEmail);
    }

    private async Task AddMemberByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            StatusMessage = "Ð˜Ð¼ÐµÐ¹Ð»ÑŠÑ‚ Ðµ Ð·Ð°Ð´ÑŠÐ»Ð¶Ð¸Ñ‚ÐµÐ»ÐµÐ½.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var success = await _apiService.PostAsync(
                $"Albums/{AlbumId}/members",
                new AddAlbumMemberDto
                {
                    UserEmail = email.Trim(),
                    CanUpload = true
                });

            if (success)
            {
                MemberEmail = string.Empty;
                SelectedFriend = null;
                await Load();
                StatusMessage = "Ð§Ð»ÐµÐ½ÑŠÑ‚ Ðµ Ð´Ð¾Ð±Ð°Ð²ÐµÐ½.";
            }
            else
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Ð§Ð»ÐµÐ½ÑŠÑ‚ Ð½Ðµ Ð±ÐµÑˆÐµ Ð´Ð¾Ð±Ð°Ð²ÐµÐ½."
                    : _apiService.LastErrorMessage;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddLetter()
    {
        if (string.IsNullOrWhiteSpace(LetterText))
        {
            StatusMessage = "Ð¢ÐµÐºÑÑ‚ÑŠÑ‚ Ðµ Ð·Ð°Ð´ÑŠÐ»Ð¶Ð¸Ñ‚ÐµÐ»ÐµÐ½.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiService.PostAsync<CreateLetterMediaDto, MediaResponseDto>(
                $"Media/album/{AlbumId}/letter",
                new CreateLetterMediaDto { Text = LetterText });

            if (result != null)
            {
                MediaItems.Insert(0, new AlbumMediaItemViewModel(result));
                LetterText = string.Empty;
                StatusMessage = "Ð¡Ð¿Ð¾Ð¼ÐµÐ½Ð° Ðµ Ð´Ð¾Ð±Ð°Ð²ÐµÐ½.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PickPhoto()
    {
        if (AlbumId <= 0) return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Ð˜Ð·Ð±ÐµÑ€Ð¸ ÑÐ½Ð¸Ð¼ÐºÐ°"
            });

            if (file == null) return;

            await using var stream = await file.OpenReadAsync();
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "image/jpeg" : file.ContentType;

            var result = await _apiService.PostFileAsync<MediaResponseDto>(
                $"Media/album/{AlbumId}/photo",
                stream, file.FileName, contentType);

            if (result != null)
            {
                MediaItems.Insert(0, new AlbumMediaItemViewModel(result));
                StatusMessage = "Ð¡Ð½Ð¸Ð¼ÐºÐ°Ñ‚Ð° Ðµ ÐºÐ°Ñ‡ÐµÐ½Ð°.";
            }
            else
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Ð¡Ð½Ð¸Ð¼ÐºÐ°Ñ‚Ð° Ð½Ðµ Ð±ÐµÑˆÐµ ÐºÐ°Ñ‡ÐµÐ½Ð°."
                    : _apiService.LastErrorMessage;
            }
        }
        catch (FeatureNotSupportedException)
        {
            StatusMessage = "ÐšÐ°Ð¼ÐµÑ€Ð°Ñ‚Ð° Ð½Ðµ ÑÐµ Ð¿Ð¾Ð´Ð´ÑŠÑ€Ð¶Ð° Ð½Ð° Ñ‚Ð¾Ð²Ð° ÑƒÑÑ‚Ñ€Ð¾Ð¹ÑÑ‚Ð²Ð¾.";
        }
        catch (PermissionException)
        {
            StatusMessage = "ÐÑÐ¼Ð°Ñˆ Ñ€Ð°Ð·Ñ€ÐµÑˆÐµÐ½Ð¸Ðµ Ð·Ð° Ð´Ð¾ÑÑ‚ÑŠÐ¿ Ð´Ð¾ ÑÐ½Ð¸Ð¼ÐºÐ¸Ñ‚Ðµ.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task Back()
    {
        await Shell.Current.GoToAsync("..");
    }
}

public class AlbumMediaItemViewModel
{
    private readonly MediaResponseDto _media;

    public int Id => _media.Id;
    public string Url => ApiService.ToDeviceUrl(_media.Url);
    public MediaType Type => _media.Type;
    public DateTime UploadedAt => _media.UploadedAt;
    public string UploadedByName => _media.UploadedByName;
    public bool IsImage => _media.Type == MediaType.Image;
    public bool IsLetter => _media.Type == MediaType.Letter;
    public bool IsLockedLetter => IsLetter && _media.IsLocked;
    public bool IsUnlockedLetter => IsLetter && !_media.IsLocked;

    public string CountdownText
    {
        get
        {
            if (!_media.UnlockAt.HasValue) return string.Empty;
            var diff = _media.UnlockAt.Value.ToLocalTime() - DateTime.Now;
            if (diff <= TimeSpan.Zero) return string.Empty;
            return $"{(int)diff.TotalDays}Ð” {diff.Hours}Ð§ {diff.Minutes}Ðœ";
        }
    }

    public string UnlockDateText
    {
        get
        {
            if (!_media.UnlockAt.HasValue) return string.Empty;
            return $"ÐžÑ‚Ð²Ð°Ñ€Ñ ÑÐµ Ð½Ð° {_media.UnlockAt.Value.ToLocalTime():dd.MM.yyyy} Ð² {_media.UnlockAt.Value.ToLocalTime():HH:mm}";
        }
    }

    public string UploadedAtText =>
        $"ÐÐ°Ð¿Ð¸ÑÐ°Ð½Ð¾ Ð½Ð° {UploadedAt.ToLocalTime():dd MMM yyyy}";

    public AlbumMediaItemViewModel(MediaResponseDto media)
    {
        _media = media;
    }

    public class AddPhotoPlaceholderViewModel
    {
        public bool IsAddButton => true;
        public bool IsImage => false;
        public bool IsLockedLetter => false;
        public bool IsUnlockedLetter => false;
    }
}
