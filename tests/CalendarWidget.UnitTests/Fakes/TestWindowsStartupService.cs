using CalendarWidget.Core.Interfaces;

namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IWindowsStartupService"/> for unit tests.
/// </summary>
public sealed class TestWindowsStartupService : IWindowsStartupService
{
    public bool IsStartupEnabledValue { get; set; }
    public bool ReturnSuccessOnSet { get; set; } = true;
    public int SetCallCount { get; private set; }

    public bool IsStartupEnabled() => IsStartupEnabledValue;

    public bool SetStartup(bool enable)
    {
        SetCallCount++;
        if (!ReturnSuccessOnSet)
            return false;

        IsStartupEnabledValue = enable;
        return true;
    }
}
