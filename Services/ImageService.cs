using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace ImgViewer.Services;

public class ImageService : IImageService
{
    private static readonly string[] _supportedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".heic", ".heif"
    ];

    private const int ThumbnailDecodeSize = 256;

    // 多数のタブを一度に開いたとき、サムネイルのデコードが並列に走りすぎないよう制限する
    private static readonly SemaphoreSlim ThumbnailThrottle = new(2);

    private static readonly string[] OrientationQueries =
    [
        "/app1/ifd/{ushort=274}", // JPEG
        "/ifd/{ushort=274}"       // TIFF
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

    public async Task<BitmapSource> LoadThumbnailAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await ThumbnailThrottle.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                var cachePath = ThumbnailCache.GetCachePath(filePath);
                if (cachePath is not null && ThumbnailCache.TryLoad(cachePath) is { } cached)
                {
                    return cached;
                }

                var thumbnail = LoadImage(filePath, ThumbnailDecodeSize);
                if (cachePath is not null)
                {
                    ThumbnailCache.Save(cachePath, thumbnail);
                }
                return thumbnail;
            }, cancellationToken);
        }
        finally
        {
            ThumbnailThrottle.Release();
        }
    }

    private static BitmapSource LoadImage(string filePath, int? maxDecodeWidth)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext is ".webp" or ".heic" or ".heif")
        {
            return LoadWithMagick(filePath, maxDecodeWidth);
        }

        return LoadWithWpf(filePath, maxDecodeWidth);
    }

    private static BitmapSource LoadWithWpf(string filePath, int? maxDecodeWidth)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        // WPF は同じ URI の画像をプロセス内でキャッシュするため、書き換えられたファイルが古いまま表示されるのを防ぐ
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        if (maxDecodeWidth.HasValue)
        {
            bitmap.DecodePixelWidth = maxDecodeWidth.Value;
        }
        bitmap.EndInit();
        bitmap.Freeze();
        return ApplyExifOrientation(bitmap, ReadExifOrientation(filePath));
    }

    private static int ReadExifOrientation(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            if (decoder.Frames[0].Metadata is not BitmapMetadata metadata)
            {
                return 1;
            }

            foreach (var query in OrientationQueries)
            {
                if (metadata.ContainsQuery(query) && metadata.GetQuery(query) is IConvertible value)
                {
                    return value.ToInt32(null);
                }
            }
        }
        catch
        {
            // BMP などメタデータを持たない形式では例外になる
        }

        return 1;
    }

    // EXIF の Orientation (1-8) に従って、表示すべき向きに変換する
    private static BitmapSource ApplyExifOrientation(BitmapSource source, int orientation)
    {
        Transform? transform = orientation switch
        {
            2 => new ScaleTransform(-1, 1),
            3 => new RotateTransform(180),
            4 => new ScaleTransform(1, -1),
            5 => new TransformGroup { Children = new TransformCollection { new ScaleTransform(-1, 1), new RotateTransform(270) } },
            6 => new RotateTransform(90),
            7 => new TransformGroup { Children = new TransformCollection { new ScaleTransform(-1, 1), new RotateTransform(90) } },
            8 => new RotateTransform(270),
            _ => null
        };

        if (transform is null)
        {
            return source;
        }

        var oriented = new TransformedBitmap(source, transform);
        oriented.Freeze();
        return oriented;
    }

    private static BitmapSource LoadWithMagick(string filePath, int? maxSize)
    {
        using var image = new MagickImage(filePath);
        image.AutoOrient();
        if (maxSize.HasValue)
        {
            image.Thumbnail(new MagickGeometry((uint)maxSize.Value, (uint)maxSize.Value) { Greater = true });
        }

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
