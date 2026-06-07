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
    public ObservableCollection<FriendItemViewModel> FilteredFriends { get; } = new();
    public ObservableCollection<AlbumMediaItemViewModel> PhotoItems { get; } = new();
    public ObservableCollection<AlbumMediaItemViewModel> RestPhotos { get; } = new();
    public ObservableCollection<AlbumMediaItemViewModel> LetterItems { get; } = new();
    public ObservableCollection<AlbumMediaItemViewModel> RestLetters { get; } = new();
    public ObservableCollection<AlbumMemberResponseDto> RestMembers { get; } = new();

    public bool HasPhotos => PhotoItems.Count > 0;
    public AlbumMediaItemViewModel? FirstPhoto => PhotoItems.Count > 0 ? PhotoItems[0] : null;
    public bool HasFirstLetter => LetterItems.Count > 0;
    public AlbumMediaItemViewModel? FirstLetter => LetterItems.Count > 0 ? LetterItems[0] : null;
    public bool HasFirstMember => Members.Count > 0;
    public AlbumMemberResponseDto? FirstMember => Members.Count > 0 ? Members[0] : null;

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

    private string _memberSearchText = string.Empty;
    public string MemberSearchText
    {
        get => _memberSearchText;
        set { _memberSearchText = value; OnPropertyChanged(); FilterFriends(); }
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

    private bool _isAddMemoryPopupVisible;
    public bool IsAddMemoryPopupVisible
    {
        get => _isAddMemoryPopupVisible;
        set { _isAddMemoryPopupVisible = value; OnPropertyChanged(); }
    }

    private bool _isAddMemberPopupVisible;
    public bool IsAddMemberPopupVisible
    {
        get => _isAddMemberPopupVisible;
        set { _isAddMemberPopupVisible = value; OnPropertyChanged(); }
    }

    private bool _isLetterPopupVisible;
    public bool IsLetterPopupVisible
    {
        get => _isLetterPopupVisible;
        set { _isLetterPopupVisible = value; OnPropertyChanged(); }
    }

    private bool _isRenamePopupVisible;
    public bool IsRenamePopupVisible
    {
        get => _isRenamePopupVisible;
        set { _isRenamePopupVisible = value; OnPropertyChanged(); }
    }

    private bool _isEditLetterPopupVisible;
    public bool IsEditLetterPopupVisible
    {
        get => _isEditLetterPopupVisible;
        set { _isEditLetterPopupVisible = value; OnPropertyChanged(); }
    }

    private string _openedLetterText = string.Empty;
    public string OpenedLetterText
    {
        get => _openedLetterText;
        set { _openedLetterText = value; OnPropertyChanged(); }
    }

    private string _openedLetterAuthor = string.Empty;
    public string OpenedLetterAuthor
    {
        get => _openedLetterAuthor;
        set { _openedLetterAuthor = value; OnPropertyChanged(); }
    }

    private string _openedLetterDate = string.Empty;
    public string OpenedLetterDate
    {
        get => _openedLetterDate;
        set { _openedLetterDate = value; OnPropertyChanged(); }
    }

    private string _newAlbumTitle = string.Empty;
    public string NewAlbumTitle
    {
        get => _newAlbumTitle;
        set { _newAlbumTitle = value; OnPropertyChanged(); }
    }

    private string _editLetterText = string.Empty;
    public string EditLetterText
    {
        get => _editLetterText;
        set { _editLetterText = value; OnPropertyChanged(); }
    }

    private AlbumMediaItemViewModel? _editingLetter;

    // ── Команди ──
    public IRelayCommand LoadCommand => new AsyncRelayCommand(Load);
    public IRelayCommand AddMemberCommand => new AsyncRelayCommand(AddMember);
    public IRelayCommand AddSelectedFriendCommand => new AsyncRelayCommand(AddSelectedFriend);
    public IRelayCommand LoadFriendsCommand => new AsyncRelayCommand(LoadFriends);
    public IRelayCommand AddLetterCommand => new AsyncRelayCommand(AddLetter);
    public IRelayCommand PickPhotoCommand => new AsyncRelayCommand(PickPhoto);
    public IRelayCommand BackCommand => new AsyncRelayCommand(Back);
    public IRelayCommand RenameAlbumCommand => new AsyncRelayCommand(RenameAlbum);
    public IRelayCommand SaveEditLetterCommand => new AsyncRelayCommand(SaveEditLetter);

    public IRelayCommand<AlbumMediaItemViewModel> DeletePhotoCommand =>
        new AsyncRelayCommand<AlbumMediaItemViewModel>(DeletePhoto);

    public IRelayCommand<AlbumMediaItemViewModel> OpenLetterCommand =>
        new RelayCommand<AlbumMediaItemViewModel>(letter =>
        {
            if (letter == null || !letter.IsUnlockedLetter) return;
            OpenedLetterText = letter.LetterContent;
            OpenedLetterAuthor = letter.UploadedByName;
            OpenedLetterDate = letter.UploadedAtText;
            IsLetterPopupVisible = true;
        });

    public IRelayCommand<AlbumMediaItemViewModel> OpenEditLetterCommand =>
        new RelayCommand<AlbumMediaItemViewModel>(letter =>
        {
            if (letter == null || !letter.IsMyLetter) return;
            _editingLetter = letter;
            EditLetterText = letter.LetterContent;
            StatusMessage = string.Empty;
            IsEditLetterPopupVisible = true;
        });

    public IRelayCommand CloseLetterPopupCommand => new RelayCommand(() =>
        IsLetterPopupVisible = false);

    public IRelayCommand CloseEditLetterPopupCommand => new RelayCommand(() =>
    {
        IsEditLetterPopupVisible = false;
        EditLetterText = string.Empty;
        _editingLetter = null;
        StatusMessage = string.Empty;
    });

    public IRelayCommand OpenRenamePopupCommand => new RelayCommand(() =>
    {
        NewAlbumTitle = AlbumTitle;
        StatusMessage = string.Empty;
        IsRenamePopupVisible = true;
    });

    public IRelayCommand CloseRenamePopupCommand => new RelayCommand(() =>
    {
        IsRenamePopupVisible = false;
        NewAlbumTitle = string.Empty;
        StatusMessage = string.Empty;
    });

    public IRelayCommand OpenAddMemoryPopupCommand => new RelayCommand(() =>
    {
        StatusMessage = string.Empty;
        IsAddMemoryPopupVisible = true;
    });

    public IRelayCommand CloseAddMemoryPopupCommand => new RelayCommand(() =>
    {
        IsAddMemoryPopupVisible = false;
        LetterText = string.Empty;
        StatusMessage = string.Empty;
    });

    public IRelayCommand OpenAddMemberPopupCommand => new RelayCommand(() =>
    {
        MemberSearchText = string.Empty;
        FilterFriends();
        StatusMessage = string.Empty;
        IsAddMemberPopupVisible = true;
    });

    public IRelayCommand CloseAddMemberPopupCommand => new RelayCommand(() =>
    {
        IsAddMemberPopupVisible = false;
        MemberSearchText = string.Empty;
        MemberEmail = string.Empty;
        StatusMessage = string.Empty;
    });

    public IRelayCommand<FriendItemViewModel> AddFriendAsMemberCommand =>
        new AsyncRelayCommand<FriendItemViewModel>(async friend =>
        {
            if (friend == null) return;
            await AddMemberByEmail(friend.Email);
            if (StatusMessage == "Членът е добавен.")
            {
                IsAddMemberPopupVisible = false;
                MemberSearchText = string.Empty;
            }
        });

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

    private void FilterFriends()
    {
        FilteredFriends.Clear();
        var search = MemberSearchText.Trim().ToLower();
        foreach (var f in Friends)
        {
            if (string.IsNullOrEmpty(search) ||
                f.FullName.ToLower().Contains(search) ||
                f.Email.ToLower().Contains(search))
                FilteredFriends.Add(f);
        }
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
            LetterItems.Clear();
            RestLetters.Clear();

            if (media != null)
            {
                foreach (var item in media)
                    MediaItems.Add(new AlbumMediaItemViewModel(item));

                var photos = media.Where(m => m.Type == MediaType.Image).ToList();
                foreach (var item in photos)
                    PhotoItems.Add(new AlbumMediaItemViewModel(item));
                foreach (var item in photos.Skip(1))
                    RestPhotos.Add(new AlbumMediaItemViewModel(item));

                var letters = media.Where(m => m.Type == MediaType.Letter && !m.IsLocked).ToList();
                foreach (var item in letters)
                    LetterItems.Add(new AlbumMediaItemViewModel(item));
                foreach (var item in letters.Skip(1))
                    RestLetters.Add(new AlbumMediaItemViewModel(item));

                OnPropertyChanged(nameof(HasPhotos));
                OnPropertyChanged(nameof(FirstPhoto));
                OnPropertyChanged(nameof(HasFirstLetter));
                OnPropertyChanged(nameof(FirstLetter));
            }

            var members = await _apiService.GetAsync<List<AlbumMemberResponseDto>>($"Albums/{AlbumId}/members");
            Members.Clear();
            RestMembers.Clear();
            if (members != null)
            {
                foreach (var m in members)
                    Members.Add(m);
                foreach (var m in members.Skip(1))
                    RestMembers.Add(m);
            }

            OnPropertyChanged(nameof(HasFirstMember));
            OnPropertyChanged(nameof(FirstMember));

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

        if (result == null) return;

        var existingMemberIds = Members.Select(m => m.UserId).ToHashSet();

        foreach (var friend in result
            .GroupBy(f => f.UserId)
            .Select(g => g.First())
            .Where(f => !existingMemberIds.Contains(f.UserId))
            .OrderBy(f => f.FullName))
            Friends.Add(new FriendItemViewModel(friend));

        if (SelectedFriend != null && !Friends.Any(f => f.UserId == SelectedFriend.UserId))
            SelectedFriend = null;

        FilterFriends();
    }

    private async Task AddSelectedFriend()
    {
        if (SelectedFriend == null)
        {
            StatusMessage = "Избери приятел първо.";
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
                    UserEmail = email.Trim(),
                    CanUpload = true
                });

            if (success)
            {
                MemberEmail = string.Empty;
                SelectedFriend = null;
                await Load();
                StatusMessage = "Членът е добавен.";
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
                var vm = new AlbumMediaItemViewModel(result);
                MediaItems.Insert(0, vm);

                if (LetterItems.Count > 0)
                    RestLetters.Insert(0, LetterItems[0]);
                LetterItems.Insert(0, vm);

                OnPropertyChanged(nameof(HasFirstLetter));
                OnPropertyChanged(nameof(FirstLetter));

                LetterText = string.Empty;
                IsAddMemoryPopupVisible = false;
                StatusMessage = "Споменът е добавен.";
            }
            else
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Споменът не беше добавен."
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

    private async Task SaveEditLetter()
    {
        if (_editingLetter == null || string.IsNullOrWhiteSpace(EditLetterText)) return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var success = await _apiService.PutAsync(
                $"Media/{_editingLetter.Id}/letter",
                new CreateLetterMediaDto { Text = EditLetterText.Trim() });

            if (success)
            {
                IsEditLetterPopupVisible = false;
                EditLetterText = string.Empty;
                _editingLetter = null;
                await Load();
            }
            else
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Писмото не беше запазено."
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

    private async Task DeletePhoto(AlbumMediaItemViewModel? photo)
    {
        if (photo == null) return;

        bool confirmed = await Shell.Current.DisplayAlert(
            "Изтрий снимка",
            "Сигурна ли си, че искаш да изтриеш тази снимка?",
            "Изтрий", "Откажи");

        if (!confirmed) return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var success = await _apiService.DeleteAsync($"Media/{photo.Id}");
            if (success)
            {
                MediaItems.Remove(photo);
                PhotoItems.Remove(photo);
                RestPhotos.Clear();
                foreach (var p in PhotoItems.Skip(1))
                    RestPhotos.Add(p);

                OnPropertyChanged(nameof(HasPhotos));
                OnPropertyChanged(nameof(FirstPhoto));
                StatusMessage = "Снимката е изтрита.";
            }
            else
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Снимката не беше изтрита."
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

    private async Task RenameAlbum()
    {
        if (string.IsNullOrWhiteSpace(NewAlbumTitle)) return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var success = await _apiService.PutAsync(
                $"Albums/{AlbumId}",
                new { Title = NewAlbumTitle.Trim() });

            if (success)
            {
                AlbumTitle = NewAlbumTitle.Trim();
                IsRenamePopupVisible = false;
                NewAlbumTitle = string.Empty;
            }
            else
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Заглавието не беше променено."
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
                var vm = new AlbumMediaItemViewModel(result);
                MediaItems.Insert(0, vm);

                if (PhotoItems.Count > 0)
                    RestPhotos.Insert(0, PhotoItems[0]);
                PhotoItems.Insert(0, vm);

                OnPropertyChanged(nameof(HasPhotos));
                OnPropertyChanged(nameof(FirstPhoto));

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
    public string Url => ApiService.ToDeviceUrl(_media.Url);
    public string LetterContent => _media.LetterText ?? string.Empty;
    public MediaType Type => _media.Type;
    public DateTime UploadedAt => _media.UploadedAt;
    public string UploadedByName => _media.UploadedByName;
    public string UploadedById => _media.UploadedById ?? string.Empty;
    public bool IsImage => _media.Type == MediaType.Image;
    public bool IsLetter => _media.Type == MediaType.Letter;
    public bool IsLockedLetter => IsLetter && _media.IsLocked;
    public bool IsUnlockedLetter => IsLetter && !_media.IsLocked;
    public bool IsMyLetter => IsLetter &&
        _media.UploadedById == Preferences.Get("user_id", string.Empty);

    public string CountdownText
    {
        get
        {
            if (!_media.UnlockAt.HasValue) return string.Empty;
            var diff = _media.UnlockAt.Value.ToLocalTime() - DateTime.Now;
            if (diff <= TimeSpan.Zero) return string.Empty;
            return $"{(int)diff.TotalDays}д {diff.Hours}ч {diff.Minutes}м";
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
}