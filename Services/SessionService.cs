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

    public async Task SaveSessionAsync(SessionData session)
    {
        session.ZoomStepPercent = NormalizeZoomStep(session.ZoomStepPercent);
        var directory = Path.GetDirectoryName(SessionFilePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(session, JsonOptions);
        await File.WriteAllTextAsync(SessionFilePath, json);
    }

    public async Task<SessionData> LoadSessionAsync()
    {
        if (!SessionExists)
        {
            return new SessionData();
        }

        var json = await File.ReadAllTextAsync(SessionFilePath);
        var session = JsonSerializer.Deserialize<SessionData>(json);

        if (session is null)
        {
            return new SessionData();
        }

        session.OpenTabs = session.OpenTabs.Where(File.Exists).ToList();
        session.ZoomStepPercent = NormalizeZoomStep(session.ZoomStepPercent);

        if (session.ActiveTabIndex >= session.OpenTabs.Count)
        {
            session.ActiveTabIndex = Math.Max(0, session.OpenTabs.Count - 1);
        }

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
        var screenWidth = System.Windows.SystemParameters.VirtualScreenWidth;
        var screenHeight = System.Windows.SystemParameters.VirtualScreenHeight;

        session.WindowWidth = Math.Max(200, Math.Min(session.WindowWidth, screenWidth));
        session.WindowHeight = Math.Max(200, Math.Min(session.WindowHeight, screenHeight));
        session.WindowLeft = Math.Max(0, Math.Min(session.WindowLeft, screenWidth - 100));
        session.WindowTop = Math.Max(0, Math.Min(session.WindowTop, screenHeight - 100));
    }

    private static int NormalizeZoomStep(int value)
    {
        return value switch
        {
            10 => 10,
            20 => 20,
            50 => 50,
            _ => 4
        };
    }
}
