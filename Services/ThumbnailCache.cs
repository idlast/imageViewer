using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace ImgViewer.Services;

/// <summary>
/// タブ見出し用サムネイルのディスクキャッシュ。
/// 起動のたびに HDD 上の元画像を丸ごと読まずに済むよう、ローカルの AppData に保存する。
/// </summary>
internal static class ThumbnailCache
{
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ImgViewer",
        "thumbs");

    private static readonly TimeSpan MaxUnusedAge = TimeSpan.FromDays(30);

    /// <summary>更新日時とサイズをキーに含めるため、元ファイルが書き換えられると別のキャッシュになる</summary>
    public static string? GetCachePath(string filePath)
    {
        var stamp = FileStamp.TryGet(filePath);
        if (stamp is null)
        {
            return null;
        }

        var key = $"{filePath.ToUpperInvariant()}|{stamp.Value.LastWriteTimeUtc.Ticks}|{stamp.Value.Length}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return Path.Combine(CacheDirectory, hash + ".png");
    }

    public static BitmapSource? TryLoad(string cachePath)
    {
        try
        {
            if (!File.Exists(cachePath))
            {
                return null;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(cachePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            bitmap.Freeze();

            // 使われているキャッシュは Prune の対象にしない
            File.SetLastWriteTimeUtc(cachePath, DateTime.UtcNow);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static void Save(string cachePath, BitmapSource thumbnail)
    {
        try
        {
            Directory.CreateDirectory(CacheDirectory);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(thumbnail));

            var tempPath = $"{cachePath}.{Guid.NewGuid():N}.tmp";
            using (var stream = File.Create(tempPath))
            {
                encoder.Save(stream);
            }
            File.Move(tempPath, cachePath, overwrite: true);
        }
        catch
        {
            // キャッシュできなくても表示には影響しない
        }
    }

    /// <summary>しばらく使われていないキャッシュを削除する</summary>
    public static void PruneUnused()
    {
        try
        {
            if (!Directory.Exists(CacheDirectory))
            {
                return;
            }

            var threshold = DateTime.UtcNow - MaxUnusedAge;
            foreach (var file in Directory.EnumerateFiles(CacheDirectory))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < threshold)
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }
}
