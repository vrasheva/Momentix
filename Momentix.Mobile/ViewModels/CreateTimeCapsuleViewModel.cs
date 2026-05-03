using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;

namespace Momentix.Mobile.ViewModels;

public partial class CreateTimeCapsuleViewModel : BaseViewModel
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
    public IRelayCommand CancelCommand => new AsyncRelayCommand(Cancel);

    public CreateTimeCapsuleViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Title is required.";
            return;
        }

        var localUnlockAt = UnlockDate.Date.Add(UnlockTime);
        if (localUnlockAt <= DateTime.Now)
        {
            ErrorMessage = "Unlock date must be in the future.";
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
                    ? "Capsule was not created."
                    : _apiService.LastErrorMessage;
                return;
            }

            Title = string.Empty;
            Description = string.Empty;
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
