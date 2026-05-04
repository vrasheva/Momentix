namespace Momentix.Mobile.ViewModels;

public class SelectedPhotoItemViewModel
{
    public FileResult File { get; }
    public string FileName => File.FileName;

    public SelectedPhotoItemViewModel(FileResult file)
    {
        File = file;
    }
}
