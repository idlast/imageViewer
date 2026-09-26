using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImgViewer.Models;
using ImgViewer.Services;

namespace ImgViewer.ViewModels;

public partial class ImageTabViewModel : ObservableObject
{
    private readonly IImageService _imageService;
    private FileStamp? _loadedStamp;
    private bool _isReloading;

    [ObservableProperty]
    private BitmapSource? _image;

    [ObservableProperty]
    private BitmapSource? _thumbnail;

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
    private int _zoomStepPercent = 50;

    [ObservableProperty]
    private bool _isActive;

    public string FilePath { get; }
    public string FileName => System.IO.Path.GetFileName(FilePath);

    public ImageTabViewModel(string filePath, IImageService imageService)
    {
        FilePath = filePath;
        _imageService = imageService;
    }

    public async Task LoadImageAsync()
    {
        if (Image is not null || IsLoading) return;

        IsLoading = true;
        LoadError = null;

        try
        {
            // 読み込み前に取得しておき、読み込み中に書き換えられた場合も次の RefreshAsync で検知できるようにする
            var stamp = await Task.Run(() => FileStamp.TryGet(FilePath));
            Image = await _imageService.LoadImageAsync(FilePath);
            _loadedStamp = stamp;
            Thumbnail ??= CreateThumbnail(Image);
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

    /// <summary>
    /// まだ読み込んでいなければ読み込み、読み込み済みでファイルが書き換えられていれば読み込み直す。
    /// </summary>
    public async Task RefreshAsync()
    {
        if (IsLoading || _isReloading) return;

        if (Image is null)
        {
            await LoadImageAsync();
            return;
        }

        _isReloading = true;
        try
        {
            var stamp = await Task.Run(() => FileStamp.TryGet(FilePath));
            if (stamp is null || stamp == _loadedStamp) return;

            var image = await _imageService.LoadImageAsync(FilePath);
            if (Image is null || image.PixelWidth != Image.PixelWidth || image.PixelHeight != Image.PixelHeight)
            {
                ResetZoom();
            }

            Image = image;
            _loadedStamp = stamp;
            Thumbnail = CreateThumbnail(image);
            LoadError = null;
        }
        catch
        {
            // 保存途中などで読めなかった場合は今の画像を表示したままにし、次回また確認する
        }
        finally
        {
            _isReloading = false;
        }
    }

    [RelayCommand]
    private void ShowInExplorer()
    {
        if (File.Exists(FilePath))
        {
            Process.Start("explorer.exe", $"/select,\"{FilePath}\"");
            return;
        }

        var directory = Path.GetDirectoryName(FilePath);
        if (Directory.Exists(directory))
        {
            Process.Start("explorer.exe", $"\"{directory}\"");
        }
    }

    /// <summary>
    /// タブ見出し用の縮小画像だけを読み込む。本体の画像はタブが選択されたときに読み込む。
    /// </summary>
    public async Task LoadThumbnailAsync()
    {
        if (Thumbnail is not null) return;

        try
        {
            var thumbnail = await _imageService.LoadThumbnailAsync(FilePath);
            Thumbnail ??= thumbnail;
        }
        catch
        {
            // サムネイルが作れなくても本体の読み込み時にエラー表示されるため無視する
        }
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

    private static BitmapSource? CreateThumbnail(BitmapSource? source)
    {
        if (source is null) return null;

        const double targetMaxDimension = 96.0;
        var maxDimension = Math.Max(source.PixelWidth, source.PixelHeight);
        if (maxDimension <= 0 || maxDimension <= targetMaxDimension)
        {
            return source;
        }

        var scale = targetMaxDimension / maxDimension;
        var transform = new ScaleTransform(scale, scale);
        var transformed = new TransformedBitmap(source, transform);
        transformed.Freeze();
        return transformed;
    }
}
