using CommunityToolkit.Mvvm.Input;
using Momentix.Data.DTOs;
using Momentix.Mobile.Services;

namespace Momentix.Mobile.ViewModels;

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

    public CreateAlbumViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            ErrorMessage = "Моля въведи име на албум.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _apiService.PostAsync<CreateAlbumDto, AlbumResponseDto>(
                "Albums",
                new CreateAlbumDto
                {
                    Title = Title,
                    Description = Description
                });

            if (result == null)
            {
                ErrorMessage = "Албумът не беше създаден.";
                return;
            }

            Title = string.Empty;
            Description = string.Empty;

            await Shell.Current.GoToAsync("//AlbumsPage");
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
        await Shell.Current.GoToAsync("//AlbumsPage");
    }
}