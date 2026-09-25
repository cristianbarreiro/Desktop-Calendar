using Microsoft.Extensions.Hosting;

namespace CalendarWidget.UnitTests.Fakes;

public sealed class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedCts = new();
    private readonly CancellationTokenSource _stoppingCts = new();
    private readonly CancellationTokenSource _stoppedCts = new();

    public int StopApplicationCallCount { get; private set; }

    public CancellationToken ApplicationStarted => _startedCts.Token;

    public CancellationToken ApplicationStopping => _stoppingCts.Token;

    public CancellationToken ApplicationStopped => _stoppedCts.Token;

    public void StopApplication()
    {
        StopApplicationCallCount++;
        _stoppingCts.Cancel();
    }
}
