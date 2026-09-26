using System.IO;
using System.Text.Json;
using ImgViewer.Models;

namespace ImgViewer.Services;

public class SessionService : ISessionService
{
    private static readonly string SessionFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ImgViewer",
        "session.json"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public bool SessionExists => File.Exists(SessionFilePath);

    public void SaveSession(SessionData session)
    {
        session.ZoomStepPercent = NormalizeZoomStep(session.ZoomStepPercent);
        var directory = Path.GetDirectoryName(SessionFilePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 書き込み途中で終了しても session.json が壊れないよう、一時ファイルに書いてから置き換える
        var tempPath = SessionFilePath + ".tmp";
        var json = JsonSerializer.Serialize(session, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, SessionFilePath, overwrite: true);
    }

    // タブの存在確認は時間がかかることがあるため、ここでは行わない（呼び出し側で行う）
    public async Task<SessionData> LoadSessionAsync()
    {
        if (!SessionExists)
        {
            return new SessionData();
        }

        SessionData? session;
        try
        {
            var json = await File.ReadAllTextAsync(SessionFilePath);
            session = JsonSerializer.Deserialize<SessionData>(json);
        }
        catch (JsonException)
        {
            return new SessionData();
        }

        if (session is null)
        {
            return new SessionData();
        }

        session.ZoomStepPercent = NormalizeZoomStep(session.ZoomStepPercent);

        ValidateWindowBounds(session);

        return session;
    }

    public async Task ClearSessionAsync()
    {
        if (SessionExists)
        {
            await Task.Run(() => File.Delete(SessionFilePath));
        }
    }

    private static void ValidateWindowBounds(SessionData session)
    {
        // プライマリより左・上のモニタは座標が負になるため、仮想スクリーンの左上を基準にする
        var screenLeft = System.Windows.SystemParameters.VirtualScreenLeft;
        var screenTop = System.Windows.SystemParameters.VirtualScreenTop;
        var screenWidth = System.Windows.SystemParameters.VirtualScreenWidth;
        var screenHeight = System.Windows.SystemParameters.VirtualScreenHeight;

        session.WindowWidth = Math.Max(200, Math.Min(session.WindowWidth, screenWidth));
        session.WindowHeight = Math.Max(200, Math.Min(session.WindowHeight, screenHeight));
        session.WindowLeft = Math.Max(screenLeft, Math.Min(session.WindowLeft, screenLeft + screenWidth - 100));
        session.WindowTop = Math.Max(screenTop, Math.Min(session.WindowTop, screenTop + screenHeight - 100));
    }

    private static int NormalizeZoomStep(int value)
    {
        return value switch
        {
            4 => 4,
            10 => 10,
            20 => 20,
            50 => 50,
            _ => 50
        };
    }
}
