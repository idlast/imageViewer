namespace ImgViewer.Models;

public class SessionData
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public double WindowWidth { get; set; } = 800;
    public double WindowHeight { get; set; } = 450;
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public bool IsMaximized { get; set; }
    public List<string> OpenTabs { get; set; } = [];
    public int ActiveTabIndex { get; set; }
    public int ZoomStepPercent { get; set; } = 4;
}
