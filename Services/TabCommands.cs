namespace ImgViewer.Services;

public abstract record TabCommand;

public sealed record OpenFilesCommand(IReadOnlyList<string> FilePaths, TabCommandSource Source) : TabCommand;

public sealed record SelectTabCommand(int Index) : TabCommand;

public sealed record SelectTabByPathCommand(string FilePath) : TabCommand;

public sealed record MoveTabCommand(int FromIndex, int ToIndex) : TabCommand;

public sealed record CloseTabCommand(int Index) : TabCommand;

public sealed record CloseTabsToRightCommand(int Index) : TabCommand;

public sealed record CloseOtherTabsCommand(int KeepIndex) : TabCommand;

/// <param name="WindowStateRestored">ウィンドウ位置・サイズ・最大化状態を反映し終えた時点で完了する</param>
public sealed record RestoreSessionCommand(TaskCompletionSource? WindowStateRestored = null) : TabCommand;

public sealed record ActivateWindowCommand : TabCommand;

public enum TabCommandSource
{
    UserAction,
    ExternalRequest,
    SessionRestore,
    Programmatic
}
