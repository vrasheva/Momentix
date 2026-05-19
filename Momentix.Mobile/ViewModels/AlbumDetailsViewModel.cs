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

    private async Task AddMember()
    {
        if (string.IsNullOrWhiteSpace(MemberEmail))
        {
            StatusMessage = "Имейлът е задължителен.";
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
                    UserEmail = MemberEmail.Trim(),
                    CanUpload = true
                });

            if (success)
            {
                StatusMessage = "Членът е добавен.";
                MemberEmail = string.Empty;
                await Load();
            }
            else
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Членът не беше добавен."
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
            StatusMessage = "Текстът е задължителен.";
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
                StatusMessage = "Спомена е добавен.";
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
                Title = "Избери снимка"
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
                StatusMessage = "Снимката е качена.";
            }
            else
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Снимката не беше качена."
                    : _apiService.LastErrorMessage;
            }
        }
        catch (FeatureNotSupportedException)
        {
            StatusMessage = "Камерата не се поддържа на това устройство.";
        }
        catch (PermissionException)
        {
            StatusMessage = "Нямаш разрешение за достъп до снимките.";
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
    public bool IsLockedLetter => IsLetter && _media.IsLocked;
    public bool IsUnlockedLetter => IsLetter && !_media.IsLocked;

    public string CountdownText
    {
        get
        {
            if (!_media.UnlockAt.HasValue) return string.Empty;
            var diff = _media.UnlockAt.Value.ToLocalTime() - DateTime.Now;
            if (diff <= TimeSpan.Zero) return string.Empty;
            return $"{(int)diff.TotalDays}Д {diff.Hours}Ч {diff.Minutes}М";
        }
    }

    public string UnlockDateText
    {
        get
        {
            if (!_media.UnlockAt.HasValue) return string.Empty;
            return $"Отваря се на {_media.UnlockAt.Value.ToLocalTime():dd.MM.yyyy} в {_media.UnlockAt.Value.ToLocalTime():HH:mm}";
        }
    }

    public string UploadedAtText =>
        $"Написано на {UploadedAt.ToLocalTime():dd MMM yyyy}";

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