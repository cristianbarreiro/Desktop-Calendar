using CalendarWidget.Presentation.Services;

namespace CalendarWidget.UnitTests.Fakes;

public sealed class TestClockService : IClockService
{
    private DateTime _now;

    public TestClockService(DateTime initialTime)
    {
        _now = initialTime;
    }

    public DateTime Now => _now;

    public DateOnly Today => DateOnly.FromDateTime(_now);

    public event EventHandler<DateTime>? TimeChanged;

    public void AdvanceTime(TimeSpan span)
    {
        _now = _now.Add(span);
        TimeChanged?.Invoke(this, _now);
    }

    public void SetTime(DateTime newTime)
    {
        _now = newTime;
        TimeChanged?.Invoke(this, _now);
    }
}
