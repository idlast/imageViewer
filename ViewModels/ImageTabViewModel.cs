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

    [ObservableProperty]
    private double _scrollOffsetX;

    [ObservableProperty]
    private double _scrollOffsetY;

    [ObservableProperty]
    private int _zoomStepPercent = 4;

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
        ZoomLevel = Math.Min(10.0, ZoomLevel * GetZoomStepFactor());
        IsZoomed = true;
    }

    public void ZoomOut()
    {
        ZoomLevel = Math.Max(0.1, ZoomLevel / GetZoomStepFactor());
        IsZoomed = true;
    }

    public void ResetZoom()
    {
        ZoomLevel = 1.0;
        IsZoomed = false;
    }

    public void UpdateScrollOffsets(double horizontal, double vertical)
    {
        ScrollOffsetX = horizontal;
        ScrollOffsetY = vertical;
    }

    private void ResetScrollOffsets()
    {
        ScrollOffsetX = 0;
        ScrollOffsetY = 0;
    }

    partial void OnIsZoomedChanged(bool value)
    {
        if (!value)
        {
            ResetScrollOffsets();
        }
    }

    private double GetZoomStepFactor()
    {
        var percent = Math.Clamp(ZoomStepPercent, 1, 100);
        return 1.0 + (percent / 100.0);
    }
}
