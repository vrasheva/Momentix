using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

[QueryProperty(nameof(AlbumId), nameof(AlbumId))]
[QueryProperty(nameof(AlbumTitle), nameof(AlbumTitle))]
public partial class AlbumDetailsViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<MediaResponseDto> MediaItems { get; } = new();

    private int _albumId;
    public int AlbumId
    {
        get => _albumId;
        set { _albumId = value; OnPropertyChanged(); }
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
    public IRelayCommand BackCommand => new AsyncRelayCommand(Back);

    public AlbumDetailsViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private async Task Load()
    {
        if (AlbumId <= 0)
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiService.GetAsync<List<MediaResponseDto>>($"Media/album/{AlbumId}");
            MediaItems.Clear();

            if (result != null)
            {
                foreach (var item in result)
                    MediaItems.Add(item);
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

    private async Task AddMember()
    {
        if (string.IsNullOrWhiteSpace(MemberEmail))
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
                    UserEmail = MemberEmail.Trim(),
                    CanUpload = CanUpload
                });

            StatusMessage = success ? "Member added." : "Member was not added.";
            if (success)
                MemberEmail = string.Empty;
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

            MediaItems.Insert(0, result);
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

    private async Task Back()
    {
        await Shell.Current.GoToAsync("..");
    }
}
