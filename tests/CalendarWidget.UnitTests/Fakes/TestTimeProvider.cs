namespace CalendarWidget.UnitTests.Fakes;

/// <summary>
/// Controllable <see cref="TimeProvider"/> for deterministic debounce and timer testing.
/// </summary>
public sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly List<TestTimer> _timers = [];
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _utcNow;

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        TestTimer timer = new(this, callback, state, dueTime, period);
        lock (_lock)
        {
            _timers.Add(timer);
        }
        return timer;
    }

    /// <summary>
    /// Advances virtual time by the specified duration, ticking registered timers and triggering expired callbacks.
    /// </summary>
    /// <param name="delta">Duration to advance.</param>
    public void Advance(TimeSpan delta)
    {
        if (delta <= TimeSpan.Zero)
        {
            return;
        }

        _utcNow += delta;

        List<TestTimer> activeTimers;
        lock (_lock)
        {
            activeTimers = [.. _timers];
        }

        foreach (TestTimer timer in activeTimers)
        {
            timer.Tick(delta);
        }
    }

    private sealed class TestTimer : ITimer
    {
        private readonly TestTimeProvider _owner;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private readonly Lock _timerLock = new();

        private TimeSpan _remainingDueTime;
        private TimeSpan _period;
        private bool _disposed;

        public TestTimer(TestTimeProvider owner, TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _owner = owner;
            _callback = callback;
            _state = state;
            _remainingDueTime = dueTime;
            _period = period;
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (_timerLock)
            {
                if (_disposed)
                {
                    return false;
                }

                _remainingDueTime = dueTime;
                _period = period;
                return true;
            }
        }

        public void Tick(TimeSpan delta)
        {
            bool shouldTrigger = false;

            lock (_timerLock)
            {
                if (_disposed || _remainingDueTime == Timeout.InfiniteTimeSpan)
                {
                    return;
                }

                _remainingDueTime -= delta;
                if (_remainingDueTime <= TimeSpan.Zero)
                {
                    shouldTrigger = true;
                    _remainingDueTime = _period == Timeout.InfiniteTimeSpan
                        ? Timeout.InfiniteTimeSpan
                        : _period;
                }
            }

            if (shouldTrigger)
            {
                _callback(_state);
            }
        }

        public void Dispose()
        {
            lock (_timerLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _remainingDueTime = Timeout.InfiniteTimeSpan;
            }

            lock (_owner._lock)
            {
                _owner._timers.Remove(this);
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
