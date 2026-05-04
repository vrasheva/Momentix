using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;
using System.Collections.ObjectModel;

namespace Momentix.Mobile.ViewModels;

public partial class CreateTimeCapsuleViewModel : BaseViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<SelectedPhotoItemViewModel> SelectedPhotos { get; } = new();

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
    public IRelayCommand PickPhotoCommand => new AsyncRelayCommand(PickPhoto);
    public IRelayCommand<SelectedPhotoItemViewModel> RemovePhotoCommand => new RelayCommand<SelectedPhotoItemViewModel>(RemovePhoto);
    public IRelayCommand CancelCommand => new AsyncRelayCommand(Cancel);

    public string SelectedPhotoCountText => SelectedPhotos.Count == 0
        ? "No photos selected"
        : $"{SelectedPhotos.Count} photo(s) selected";

    public CreateTimeCapsuleViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private async Task PickPhoto()
    {
        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select capsule photo"
            });

            if (file == null)
                return;

            SelectedPhotos.Add(new SelectedPhotoItemViewModel(file));
            OnPropertyChanged(nameof(SelectedPhotoCountText));
            ErrorMessage = string.Empty;
        }
        catch (FeatureNotSupportedException)
        {
            ErrorMessage = "Photo picker is not supported on this device.";
        }
        catch (PermissionException)
        {
            ErrorMessage = "Photo permission was denied.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void RemovePhoto(SelectedPhotoItemViewModel? photo)
    {
        if (photo == null)
            return;

        SelectedPhotos.Remove(photo);
        OnPropertyChanged(nameof(SelectedPhotoCountText));
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

            foreach (var photo in SelectedPhotos.ToList())
            {
                await using var stream = await photo.File.OpenReadAsync();
                var contentType = string.IsNullOrWhiteSpace(photo.File.ContentType)
                    ? "image/jpeg"
                    : photo.File.ContentType;

                var uploadResult = await _apiService.PostFileAsync<MediaResponseDto>(
                    $"Media/timecapsule/{result.Id}/photo",
                    stream,
                    photo.File.FileName,
                    contentType);

                if (uploadResult == null)
                {
                    ErrorMessage = string.IsNullOrWhiteSpace(_apiService.LastErrorMessage)
                        ? "Capsule was created, but a photo was not uploaded."
                        : _apiService.LastErrorMessage;
                    return;
                }
            }

            Title = string.Empty;
            Description = string.Empty;
            SelectedPhotos.Clear();
            OnPropertyChanged(nameof(SelectedPhotoCountText));
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
