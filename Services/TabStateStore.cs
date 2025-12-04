using System.Collections.Immutable;

namespace ImgViewer.Services;

public sealed class TabState
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
}

public sealed record AppState
{
    public ImmutableList<TabState> Tabs { get; init; } = [];
    public int SelectedIndex { get; init; } = -1;
    public double WindowWidth { get; init; } = 800;
    public double WindowHeight { get; init; } = 450;
    public double WindowLeft { get; init; } = 100;
    public double WindowTop { get; init; } = 100;
    public bool IsMaximized { get; init; }
    public int ZoomStepPercent { get; init; } = 4;

    public string? SelectedFilePath => SelectedIndex >= 0 && SelectedIndex < Tabs.Count
        ? Tabs[SelectedIndex].FilePath
        : null;

    public AppState WithTab(TabState tab, bool select)
    {
        var existingIndex = Tabs.FindIndex(t => t.FilePath == tab.FilePath);
        if (existingIndex >= 0)
        {
            return select ? this with { SelectedIndex = existingIndex } : this;
        }

        var newTabs = Tabs.Add(tab);
        return this with
        {
            Tabs = newTabs,
            SelectedIndex = select ? newTabs.Count - 1 : SelectedIndex
        };
    }

    public AppState WithTabRemoved(int index)
    {
        if (index < 0 || index >= Tabs.Count)
        {
            return this;
        }

        var newTabs = Tabs.RemoveAt(index);
        var newSelected = SelectedIndex;

        if (newTabs.Count == 0)
        {
            newSelected = -1;
        }
        else if (index == SelectedIndex)
        {
            newSelected = Math.Min(index, newTabs.Count - 1);
        }
        else if (index < SelectedIndex)
        {
            newSelected--;
        }

        return this with { Tabs = newTabs, SelectedIndex = newSelected };
    }

    public AppState WithTabMoved(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Tabs.Count)
        {
            return this;
        }

        var destination = Math.Clamp(toIndex, 0, Tabs.Count - 1);
        if (fromIndex == destination)
        {
            return this;
        }

        var tab = Tabs[fromIndex];
        var newTabs = Tabs.RemoveAt(fromIndex).Insert(destination, tab);
        var newSelected = SelectedIndex;

        if (SelectedIndex == fromIndex)
        {
            newSelected = destination;
        }
        else if (fromIndex < SelectedIndex && destination >= SelectedIndex)
        {
            newSelected--;
        }
        else if (fromIndex > SelectedIndex && destination <= SelectedIndex)
        {
            newSelected++;
        }

        return this with { Tabs = newTabs, SelectedIndex = newSelected };
    }

    public AppState WithSelection(int index)
    {
        if (index < 0 || index >= Tabs.Count)
        {
            return this;
        }

        return this with { SelectedIndex = index };
    }

    public AppState WithTabsClosedToRight(int keepIndex)
    {
        if (keepIndex < 0 || keepIndex >= Tabs.Count - 1)
        {
            return this;
        }

        var newTabs = Tabs.GetRange(0, keepIndex + 1).ToImmutableList();
        var newSelected = Math.Min(SelectedIndex, newTabs.Count - 1);

        return this with { Tabs = newTabs, SelectedIndex = newSelected };
    }

    public AppState WithOnlyTab(int keepIndex)
    {
        if (keepIndex < 0 || keepIndex >= Tabs.Count)
        {
            return this;
        }

        var tab = Tabs[keepIndex];
        return this with { Tabs = [tab], SelectedIndex = 0 };
    }
}

public sealed class TabStateStore
{
    private AppState _state = new();
    private readonly object _lock = new();

    public event EventHandler<StateChangedEventArgs>? StateChanged;

    public AppState State
    {
        get { lock (_lock) return _state; }
    }

    public void Apply(Func<AppState, AppState> mutation, bool isSessionRestore = false)
    {
        AppState oldState;
        AppState newState;

        lock (_lock)
        {
            oldState = _state;
            newState = mutation(oldState);
            _state = newState;
        }

        if (!ReferenceEquals(oldState, newState))
        {
            StateChanged?.Invoke(this, new StateChangedEventArgs(oldState, newState, isSessionRestore));
        }
    }

    public void SetState(AppState state, bool isSessionRestore = false)
    {
        Apply(_ => state, isSessionRestore);
    }
}

public sealed class StateChangedEventArgs : EventArgs
{
    public StateChangedEventArgs(AppState oldState, AppState newState, bool isSessionRestore = false)
    {
        OldState = oldState;
        NewState = newState;
        IsSessionRestore = isSessionRestore;
    }

    public AppState OldState { get; }
    public AppState NewState { get; }
    public bool IsSessionRestore { get; }
}
