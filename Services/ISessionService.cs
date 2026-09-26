using ImgViewer.Models;

namespace ImgViewer.Services;

public interface ISessionService
{
    void SaveSession(SessionData session);
    Task<SessionData> LoadSessionAsync();
    bool SessionExists { get; }
    Task ClearSessionAsync();
}
