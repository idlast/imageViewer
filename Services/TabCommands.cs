namespace ImgViewer.Services;

public abstract record TabCommand;

public sealed record OpenFilesCommand(IReadOnlyList<string> FilePaths, TabCommandSource Source) : TabCommand;

public sealed record SelectTabCommand(int Index) : TabCommand;

public sealed record SelectTabByPathCommand(string FilePath) : TabCommand;

public sealed record MoveTabCommand(int FromIndex, int ToIndex) : TabCommand;

public sealed record CloseTabCommand(int Index) : TabCommand;

public sealed record CloseTabsToRightCommand(int Index) : TabCommand;

public sealed record CloseOtherTabsCommand(int KeepIndex) : TabCommand;

public sealed record RestoreSessionCommand : TabCommand;

public sealed record SaveSessionCommand : TabCommand;

public sealed record ActivateWindowCommand : TabCommand;

public enum TabCommandSource
{
    UserAction,
    ExternalRequest,
    SessionRestore,
    Programmatic
}
