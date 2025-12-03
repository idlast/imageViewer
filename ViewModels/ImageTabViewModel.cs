using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImgViewer.Models;
using ImgViewer.Services;

namespace ImgViewer.ViewModels;

public partial class ImageTabViewModel : ObservableObject
{
    private readonly IImageService _imageService;

    [ObservableProperty]
    private BitmapSource? _image;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _loadError;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private bool _isZoomed;

    public string FilePath { get; }
    public string FileName => System.IO.Path.GetFileName(FilePath);

    public ImageTabViewModel(string filePath, IImageService imageService)
    {
        FilePath = filePath;
        _imageService = imageService;
    }

    public async Task LoadImageAsync()
    {
        if (Image is not null) return;

        IsLoading = true;
        LoadError = null;

        try
        {
            Image = await _imageService.LoadImageAsync(FilePath);
        }
        catch (Exception ex)
        {
            LoadError = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ZoomIn()
    {
        ZoomLevel = Math.Min(10.0, ZoomLevel * 1.2);
        IsZoomed = true;
    }

    public void ZoomOut()
    {
        ZoomLevel = Math.Max(0.1, ZoomLevel / 1.2);
        IsZoomed = true;
    }

    public void ResetZoom()
    {
        ZoomLevel = 1.0;
        IsZoomed = false;
    }
}
