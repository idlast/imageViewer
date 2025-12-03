using System.Windows.Media.Imaging;

namespace ImgViewer.Services;

public interface IImageService
{
    Task<BitmapSource> LoadImageAsync(string filePath, int? maxDecodeWidth = null, CancellationToken cancellationToken = default);
    bool IsSupportedFormat(string filePath);
    IReadOnlyList<string> SupportedExtensions { get; }
    string FileDialogFilter { get; }
}
