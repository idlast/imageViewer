using System.IO;

namespace ImgViewer.Services;

/// <summary>ファイルが書き換えられたかを判定するための更新日時とサイズ</summary>
public readonly record struct FileStamp(DateTime LastWriteTimeUtc, long Length)
{
    public static FileStamp? TryGet(string filePath)
    {
        try
        {
            var info = new FileInfo(filePath);
            return info.Exists ? new FileStamp(info.LastWriteTimeUtc, info.Length) : null;
        }
        catch
        {
            return null;
        }
    }
}
