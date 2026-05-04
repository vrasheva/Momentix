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
    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();

    private int _albumId;
    public int AlbumId
    {
        get => _albumId;
        set
        {
            if (_albumId == value)
                return;

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

    private bool _canUpload = true;
    public bool CanUpload
    {
        get => _canUpload;
        set { _canUpload = value; OnPropertyChanged(); }
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
    public IRelayCommand LoadFriendsCommand => new AsyncRelayCommand(LoadFriends);
    public IRelayCommand AddMemberCommand => new AsyncRelayCommand(AddMember);
    public IRelayCommand AddSelectedFriendCommand => new AsyncRelayCommand(AddSelectedFriend);
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

        if (LoadFriendsCommand.CanExecute(null))
            LoadFriendsCommand.Execute(null);
    }

    private void LoadIfReady()
    {
        if (!_isPageVisible || AlbumId <= 0 || IsLoading)
            return;

        LoadCommand.Execute(null);
    }

    private async Task Load()
    {
        if (AlbumId <= 0)
        {
            StatusMessage = "Album was not opened correctly.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiService.GetAsync<List<MediaResponseDto>>($"Media/album/{AlbumId}");
            MediaItems.Clear();

            if (result != null)
            {
                foreach (var item in result)
                    MediaItems.Add(new AlbumMediaItemViewModel(item));
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

    private async Task LoadFriends()
    {
        try
        {
            var result = await _apiService.GetAsync<List<FriendResponseDto>>("Friends");
            Friends.Clear();

            if (result == null)
                return;

            foreach (var friend in result
                .GroupBy(f => f.UserId)
                .Select(g => g.First())
                .OrderBy(f => f.FullName))
                Friends.Add(new FriendItemViewModel(friend));
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private async Task AddMember()
    {
        if (string.IsNullOrWhiteSpace(MemberEmail))
        {
            StatusMessage = "Email is required.";
            return;
        }

        await AddMemberByEmail(MemberEmail.Trim());
    }

    private async Task AddSelectedFriend()
    {
        if (SelectedFriend == null)
        {
            StatusMessage = "Select a friend first.";
            return;
        }

        await AddMemberByEmail(SelectedFriend.Email);
    }

    private async Task AddMemberByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            StatusMessage = "Email is required.";
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
                    UserEmail = email,
                    CanUpload = CanUpload
                });

            StatusMessage = success
                ? "Member added."
                : string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Member was not added."
                    : _apiService.LastErrorMessage;
            if (success)
            {
                MemberEmail = string.Empty;
                SelectedFriend = null;
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
            StatusMessage = "Memory text is required.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiService.PostAsync<CreateLetterMediaDto, MediaResponseDto>(
                $"Media/album/{AlbumId}/letter",
                new CreateLetterMediaDto { Text = LetterText });

            if (result == null)
            {
                StatusMessage = "Memory was not added.";
                return;
            }

            MediaItems.Insert(0, new AlbumMediaItemViewModel(result));
            LetterText = string.Empty;
            StatusMessage = "Memory added.";
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
        if (AlbumId <= 0)
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select album photo"
            });

            if (file == null)
                return;

            await using var stream = await file.OpenReadAsync();
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "image/jpeg"
                : file.ContentType;

            var result = await _apiService.PostFileAsync<MediaResponseDto>(
                $"Media/album/{AlbumId}/photo",
                stream,
                file.FileName,
                contentType);

            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Photo was not uploaded."
                    : _apiService.LastErrorMessage;
                return;
            }

            MediaItems.Insert(0, new AlbumMediaItemViewModel(result));
            StatusMessage = "Photo uploaded.";
        }
        catch (FeatureNotSupportedException)
        {
            StatusMessage = "Photo picker is not supported on this device.";
        }
        catch (PermissionException)
        {
            StatusMessage = "Photo permission was denied.";
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
    public string Url => _media.Url;
    public MediaType Type => _media.Type;
    public DateTime UploadedAt => _media.UploadedAt;
    public string UploadedByName => _media.UploadedByName;
    public bool IsImage => _media.Type == MediaType.Image;
    public bool IsLetter => _media.Type == MediaType.Letter;

    public AlbumMediaItemViewModel(MediaResponseDto media)
    {
        _media = media;
    }
}
