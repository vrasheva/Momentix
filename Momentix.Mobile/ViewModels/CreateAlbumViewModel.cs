using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Momentix.Mobile.ViewModels;

public class FriendInviteViewModel : INotifyPropertyChanged
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Initials => FullName.Length >= 2
        ? $"{FullName[0]}{FullName.Split(' ').LastOrDefault()?[0]}"
        : FullName.Length == 1 ? FullName[0].ToString() : "?";

    private bool _isInvited;
    public bool IsInvited
    {
        get => _isInvited;
        set
        {
            _isInvited = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanUpload));
        }
    }

    private bool _canUpload = true;
    public bool CanUpload
    {
        get => _canUpload;
        set
        {
            _canUpload = value;
            OnPropertyChanged();
        }
    }

    public FriendInviteViewModel(FriendResponseDto dto)
    {
        UserId = dto.UserId;
        FullName = dto.FullName;
        Email = dto.Email;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class CreateAlbumViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

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

    private string _letterText = string.Empty;
    public string LetterText
    {
        get => _letterText;
        set { _letterText = value; OnPropertyChanged(); }
    }

    private bool _hasLetter;
    public bool HasLetter
    {
        get => _hasLetter;
        set { _hasLetter = value; OnPropertyChanged(); }
    }

    private bool _lockLetter;
    public bool LockLetter
    {
        get => _lockLetter;
        set { _lockLetter = value; OnPropertyChanged(); }
    }

    private DateTime _letterUnlockDate = DateTime.Now.AddDays(30);
    public DateTime LetterUnlockDate
    {
        get => _letterUnlockDate;
        set { _letterUnlockDate = value; OnPropertyChanged(); }
    }

    private TimeSpan _letterUnlockTime = new TimeSpan(22, 0, 0);
    public TimeSpan LetterUnlockTime
    {
        get => _letterUnlockTime;
        set { _letterUnlockTime = value; OnPropertyChanged(); }
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

    public ObservableCollection<string> SelectedPhotos { get; } = new();
    public int SelectedPhotosCount => SelectedPhotos.Count;

    public ObservableCollection<FriendInviteViewModel> Friends { get; } = new();

    public IRelayCommand SaveCommand => new AsyncRelayCommand(Save);
    public IRelayCommand CancelCommand => new AsyncRelayCommand(Cancel);
    public IRelayCommand LoadFriendsCommand => new AsyncRelayCommand(LoadFriends);
    public IRelayCommand PickPhotoCommand => new AsyncRelayCommand(PickPhoto);

    public CreateAlbumViewModel(ApiService apiService)
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
            var result = await _apiService.GetAsync<List<FriendResponseDto>>("Friends");
            Friends.Clear();
            if (result != null)
                foreach (var f in result.GroupBy(f => f.UserId).Select(g => g.First()))
                    Friends.Add(new FriendInviteViewModel(f));
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
            
            if (file == null)
                return;

            SelectedPhotos.Add(file.FullPath);
            OnPropertyChanged(nameof(SelectedPhotosCount));
        }
        catch { }
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Моля въведи ime на албума.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var album = await _apiService.PostAsync<CreateAlbumDto, AlbumResponseDto>(
                "Albums",
                new CreateAlbumDto { Title = Title, Description = Description });

            if (album == null)
            {
                ErrorMessage = "Албумът не беше създаден.";
                return;
            }

            // Качи снимките
            foreach (var photoPath in SelectedPhotos)
            {
                try
                {
                    await using var stream = File.OpenRead(photoPath);
                    var fileName = Path.GetFileName(photoPath);
                    var ext = Path.GetExtension(photoPath).ToLowerInvariant();
                    var contentType = ext switch
                    {
                        ".png" => "image/png",
                        ".webp" => "image/webp",
                        ".gif" => "image/gif",
                        _ => "image/jpeg"
                    };

                    await _apiService.PostFileAsync<MediaResponseDto>(
                        $"Media/album/{album.Id}/photo",
                        stream, fileName, contentType);
                }
                catch { }
            }

            // Добави членове
            foreach (var friend in Friends.Where(f => f.IsInvited))
            {
                System.Diagnostics.Debug.WriteLine($"Adding member: {friend.Email}, CanUpload: {friend.CanUpload}");

                var success = await _apiService.PostAsync(
                    $"Albums/{album.Id}/members",
                    new AddAlbumMemberDto
                    {
                        UserEmail = friend.Email,
                        CanUpload = friend.CanUpload
                    });

                System.Diagnostics.Debug.WriteLine($"Result: {success}");
            }

            // Добави писмо
            if (HasLetter && !string.IsNullOrWhiteSpace(LetterText))
            {
                DateTime? unlockAt = null;
                if (LockLetter)
                {
                    var unlockDate = LetterUnlockDate.Date + LetterUnlockTime;
                    unlockAt = DateTime.SpecifyKind(unlockDate, DateTimeKind.Local).ToUniversalTime();
                }

                await _apiService.PostAsync<CreateLetterMediaDto, MediaResponseDto>(
                    $"Media/album/{album.Id}/letter",
                    new CreateLetterMediaDto { Text = LetterText, UnlockAt = unlockAt });
            }

            Title = string.Empty;
            Description = string.Empty;
            LetterText = string.Empty;
            HasLetter = false;
            LockLetter = false;
            SelectedPhotos.Clear();
            OnPropertyChanged(nameof(SelectedPhotosCount));

            await Shell.Current.GoToAsync("..");
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

    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
}