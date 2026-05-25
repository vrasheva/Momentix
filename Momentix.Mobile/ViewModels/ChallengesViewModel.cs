using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

public partial class ChallengesViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<ChallengeSubmissionItemViewModel> Submissions { get; } = new();

    private ChallengeResponseDto? _activeChallenge;
    public ChallengeResponseDto? ActiveChallenge
    {
        get => _activeChallenge;
        set
        {
            _activeChallenge = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ChallengeTitle));
            OnPropertyChanged(nameof(RevealText));
        }
    }

    public string ChallengeTitle => ActiveChallenge?.Description ?? "Няма активно предизвикателство";

    public string RevealText => ActiveChallenge == null
        ? string.Empty
        : $"Разкриване: {ActiveChallenge.RevealAt.ToLocalTime():HH:mm}";

    private string _submissionText = string.Empty;
    public string SubmissionText
    {
        get => _submissionText;
        set { _submissionText = value; OnPropertyChanged(); }
    }

    private FileResult? _selectedPhoto;
    public FileResult? SelectedPhoto
    {
        get => _selectedPhoto;
        set
        {
            _selectedPhoto = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedPhotoName));
            OnPropertyChanged(nameof(HasSelectedPhoto));
        }
    }

    public string SelectedPhotoName => SelectedPhoto == null
        ? "Няма избрана снимка"
        : SelectedPhoto.FileName;

    public bool HasSelectedPhoto => SelectedPhoto != null;

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
    public IRelayCommand PickPhotoCommand => new AsyncRelayCommand(PickPhoto);
    public IRelayCommand SubmitCommand => new AsyncRelayCommand(Submit);
    public IRelayCommand<ChallengeSubmissionItemViewModel> VoteCommand =>
        new AsyncRelayCommand<ChallengeSubmissionItemViewModel>(Vote);
    public IRelayCommand GoToProfileCommand => new AsyncRelayCommand(async () =>
        await Shell.Current.GoToAsync("ProfilePage"));

    public ChallengesViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private async Task PickPhoto()
    {
        try
        {
            SelectedPhoto = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Избери снимка"
            });

            if (SelectedPhoto != null)
                StatusMessage = string.Empty;
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
    }

    private async Task Load()
    {
        if (IsLoading) return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            ActiveChallenge = await _apiService.GetAsync<ChallengeResponseDto>("Challenges/active");
            await LoadSubmissions();
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

    private async Task LoadSubmissions()
    {
        Submissions.Clear();
        if (ActiveChallenge == null) return;

        var result = await _apiService.GetAsync<List<ChallengeSubmissionResponseDto>>(
            $"Challenges/{ActiveChallenge.Id}/submissions");

        if (result == null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                ? "Публикациите не можаха да се заредят."
                : _apiService.LastErrorMessage;
            return;
        }

        foreach (var submission in result
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .OrderByDescending(s => s.SubmittedAt))
        {
            submission.MediaUrl = ApiService.ToDeviceUrl(submission.MediaUrl);
            Submissions.Add(new ChallengeSubmissionItemViewModel(submission));
        }
    }

    private async Task Submit()
    {
        if (IsLoading) return;
        if (ActiveChallenge == null) { StatusMessage = "Няма активно предизвикателство."; return; }
        if (SelectedPhoto == null) { StatusMessage = "Избери снимка първо."; return; }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            await using var stream = await SelectedPhoto.OpenReadAsync();
            var contentType = string.IsNullOrWhiteSpace(SelectedPhoto.ContentType)
                ? "image/jpeg"
                : SelectedPhoto.ContentType;

            var result = await _apiService.PostFileAsync<ChallengeSubmissionResponseDto>(
                $"Challenges/{ActiveChallenge.Id}/submissions/photo",
                stream, SelectedPhoto.FileName, contentType);

            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Публикацията не беше запазена."
                    : _apiService.LastErrorMessage;
                return;
            }

            result.MediaUrl = ApiService.ToDeviceUrl(result.MediaUrl);

            if (!Submissions.Any(s => s.Id == result.Id))
                Submissions.Insert(0, new ChallengeSubmissionItemViewModel(result));

            SubmissionText = string.Empty;
            SelectedPhoto = null;

            StatusMessage = result.AiIsSatisfied == true
                ? $"✓ Прието! {result.AiFeedback}"
                : result.AiIsSatisfied == false
                    ? $"✗ Не прието: {result.AiFeedback}"
                    : "Публикувано. Streak +1.";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    private async Task Vote(ChallengeSubmissionItemViewModel? submission)
    {
        if (submission == null) return;

        var success = await _apiService.PostAsync(
            $"Challenges/submissions/{submission.Id}/vote",
            new CreateChallengeVoteDto { SelectedOption = ChallengeTitle });

        StatusMessage = success ? "Гласът е запазен." : "Гласът не беше запазен.";
        if (success) await LoadSubmissions();
    }
}

public class ChallengeSubmissionItemViewModel
{
    private readonly ChallengeSubmissionResponseDto _dto;

    public int Id => _dto.Id;
    public string UserName => _dto.UserName;
    public string MediaUrl => _dto.MediaUrl;
    public DateTime SubmittedAt => _dto.SubmittedAt;
    public int VoteCount => _dto.VoteCount;

    public string UserInitials
    {
        get
        {
            var parts = UserName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpper();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }
    }

    public string SubmittedAtText => SubmittedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm");

    public bool HasAiFeedback => !string.IsNullOrWhiteSpace(_dto.AiFeedback);
    public string AiFeedback => _dto.AiFeedback ?? string.Empty;

    public string AiBadgeText
    {
        get
        {
            if (_dto.AiIsSatisfied == null) return "AI ?";
            return _dto.AiIsSatisfied == true ? "✓ AI" : "✗ AI";
        }
    }

    public string AiBadgeColor
    {
        get
        {
            if (_dto.AiIsSatisfied == null) return "#888888";
            return _dto.AiIsSatisfied == true ? "#16A34A" : "#DC2626";
        }
    }

    public ChallengeSubmissionItemViewModel(ChallengeSubmissionResponseDto dto)
    {
        _dto = dto;
    }
}