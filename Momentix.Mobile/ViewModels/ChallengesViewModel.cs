using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

public partial class ChallengesViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<ChallengeSubmissionResponseDto> Submissions { get; } = new();

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

    public string ChallengeTitle => ActiveChallenge?.Description ?? "No active challenge";

    public string RevealText => ActiveChallenge == null
        ? string.Empty
        : $"Reveal time: {ActiveChallenge.RevealAt.ToLocalTime():HH:mm}";

    private string _submissionText = string.Empty;
    public string SubmissionText
    {
        get => _submissionText;
        set { _submissionText = value; OnPropertyChanged(); }
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
    public IRelayCommand SubmitCommand => new AsyncRelayCommand(Submit);
    public IRelayCommand<ChallengeSubmissionResponseDto> VoteCommand => new AsyncRelayCommand<ChallengeSubmissionResponseDto>(Vote);

    public ChallengesViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private async Task Load()
    {
        if (IsLoading)
            return;

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

        if (ActiveChallenge == null)
            return;

        var result = await _apiService.GetAsync<List<ChallengeSubmissionResponseDto>>(
            $"Challenges/{ActiveChallenge.Id}/submissions");

        if (result == null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                ? "Submissions could not be loaded."
                : _apiService.LastErrorMessage;
            return;
        }

        foreach (var submission in result
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .OrderByDescending(s => s.SubmittedAt))
            Submissions.Add(submission);
    }

    private async Task Submit()
    {
        if (IsLoading)
            return;

        if (ActiveChallenge == null)
        {
            StatusMessage = "No active challenge.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SubmissionText))
        {
            StatusMessage = "Add a description or image link.";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            var result = await _apiService.PostAsync<CreateChallengeSubmissionDto, ChallengeSubmissionResponseDto>(
                $"Challenges/{ActiveChallenge.Id}/submissions",
                new CreateChallengeSubmissionDto { MediaUrl = SubmissionText });

            if (result == null)
            {
                StatusMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                    ? "Submission was not saved."
                    : _apiService.LastErrorMessage;
                return;
            }

            if (!Submissions.Any(s => s.Id == result.Id))
                Submissions.Insert(0, result);

            SubmissionText = string.Empty;
            StatusMessage = "Submission saved. Streak +1.";
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

    private async Task Vote(ChallengeSubmissionResponseDto? submission)
    {
        if (submission == null)
            return;

        var success = await _apiService.PostAsync(
            $"Challenges/submissions/{submission.Id}/vote",
            new CreateChallengeVoteDto { SelectedOption = ChallengeTitle });

        StatusMessage = success ? "Vote saved." : "Vote was not saved.";

        if (success)
            await LoadSubmissions();
    }
}
