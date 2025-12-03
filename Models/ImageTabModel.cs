using System.Windows.Media.Imaging;

namespace ImgViewer.Models;

public class ImageTabModel
{
    public required string FilePath { get; init; }
    public string FileName => System.IO.Path.GetFileName(FilePath);
    public BitmapSource? Image { get; set; }
    public double ZoomLevel { get; set; } = 1.0;
    public bool IsZoomed { get; set; }
    public bool IsLoading { get; set; }
    public string? LoadError { get; set; }
}
