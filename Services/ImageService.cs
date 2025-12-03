using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace ImgViewer.Services;

public class ImageService : IImageService
{
    private static readonly string[] _supportedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".heic", ".heif"
    ];

    public IReadOnlyList<string> SupportedExtensions => _supportedExtensions;

    public string FileDialogFilter =>
        "画像ファイル|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.tiff;*.tif;*.webp;*.heic;*.heif|すべてのファイル|*.*";

    public bool IsSupportedFormat(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return _supportedExtensions.Contains(ext);
    }

    public async Task<BitmapSource> LoadImageAsync(string filePath, int? maxDecodeWidth = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadImage(filePath, maxDecodeWidth), cancellationToken);
    }

    private static BitmapSource LoadImage(string filePath, int? maxDecodeWidth)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext is ".webp" or ".heic" or ".heif")
        {
            return LoadWithMagick(filePath);
        }

        return LoadWithWpf(filePath, maxDecodeWidth);
    }

    private static BitmapSource LoadWithWpf(string filePath, int? maxDecodeWidth)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        if (maxDecodeWidth.HasValue)
        {
            bitmap.DecodePixelWidth = maxDecodeWidth.Value;
        }
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource LoadWithMagick(string filePath)
    {
        using var image = new MagickImage(filePath);

        var width = image.Width;
        var height = image.Height;
        var stride = (int)width * 4;
        var pixels = image.ToByteArray(MagickFormat.Bgra);

        var bitmap = BitmapSource.Create(
            (int)width,
            (int)height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            pixels,
            stride
        );
        bitmap.Freeze();
        return bitmap;
    }
}
