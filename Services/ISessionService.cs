using ImgViewer.Models;

namespace ImgViewer.Services;

public interface ISessionService
{
    Task SaveSessionAsync(SessionData session);
    Task<SessionData> LoadSessionAsync();
    bool SessionExists { get; }
    Task ClearSessionAsync();
}
