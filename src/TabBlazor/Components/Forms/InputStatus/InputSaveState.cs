namespace TabBlazor;

/// <summary>Save progress shown by <see cref="InputStatus"/>.</summary>
public enum InputSaveState
{
    /// <summary>Nothing in progress; no indicator is shown.</summary>
    Idle,
    /// <summary>A save is running; a spinner is shown inside the input.</summary>
    Saving,
    /// <summary>The save succeeded; a check and a short-lived bubble are shown.</summary>
    Saved,
    /// <summary>The save failed; an alert icon and the error bubble are shown.</summary>
    Error
}
