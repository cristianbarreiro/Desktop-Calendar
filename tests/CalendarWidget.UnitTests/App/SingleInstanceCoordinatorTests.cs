using CalendarWidget.App.Services;
using FluentAssertions;

namespace CalendarWidget.UnitTests.App;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public void IsPrimary_AcquiresMutexSuccessfully_ReturnsTrueForFirstInstance()
    {
        // Arrange
        string mutexName = $"Test_Mutex_{Guid.NewGuid():N}";
        string pipeName = $"Test_Pipe_{Guid.NewGuid():N}";

        // Act
        using SingleInstanceCoordinator coordinator = new(mutexName: mutexName, pipeName: pipeName);

        // Assert
        coordinator.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void SecondInstance_WithSameMutex_IsPrimaryReturnsFalse()
    {
        // Arrange
        string mutexName = $"Test_Mutex_{Guid.NewGuid():N}";
        string pipeName = $"Test_Pipe_{Guid.NewGuid():N}";

        using SingleInstanceCoordinator primary = new(mutexName: mutexName, pipeName: pipeName);
        primary.IsPrimary.Should().BeTrue();

        // Act
        using SingleInstanceCoordinator secondary = new(mutexName: mutexName, pipeName: pipeName);

        // Assert
        secondary.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task SecondInstance_SignalsPrimary_DeliversActivationMessageOverPipe()
    {
        // Arrange
        string mutexName = $"Test_Mutex_{Guid.NewGuid():N}";
        string pipeName = $"Test_Pipe_{Guid.NewGuid():N}";

        TaskCompletionSource<bool> activatedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        using SingleInstanceCoordinator primary = new(
            onActivateRequested: () => activatedTcs.TrySetResult(true),
            mutexName: mutexName,
            pipeName: pipeName);

        primary.StartListening();

        using SingleInstanceCoordinator secondary = new(
            mutexName: mutexName,
            pipeName: pipeName);

        // Act: secondary sends activation signal to primary
        bool signalSuccess = secondary.SignalPrimary(timeoutMs: 3000);

        // Assert
        signalSuccess.Should().BeTrue();
        Task completed = await Task.WhenAny(activatedTcs.Task, Task.Delay(3000));
        completed.Should().Be(activatedTcs.Task);
        bool activated = await activatedTcs.Task;
        activated.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ReleasesMutex_AllowsSubsequentInstanceToAcquire()
    {
        // Arrange
        string mutexName = $"Test_Mutex_{Guid.NewGuid():N}";
        string pipeName = $"Test_Pipe_{Guid.NewGuid():N}";

        SingleInstanceCoordinator first = new(mutexName: mutexName, pipeName: pipeName);
        first.IsPrimary.Should().BeTrue();

        // Act: dispose the first instance
        first.Dispose();

        // Assert: second instance can now become primary
        using SingleInstanceCoordinator second = new(mutexName: mutexName, pipeName: pipeName);
        second.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void SignalPrimary_WhenNoServerListening_ReturnsFalseGracefully()
    {
        // Arrange
        string mutexName = $"Test_Mutex_{Guid.NewGuid():N}";
        string pipeName = $"Test_Pipe_{Guid.NewGuid():N}";

        using SingleInstanceCoordinator coordinator = new(mutexName: mutexName, pipeName: pipeName);

        // Act: attempt to signal with very short timeout when no listener is active
        bool result = coordinator.SignalPrimary(timeoutMs: 50);

        // Assert: fails cleanly without throwing
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RepeatedActivations_MultipleSignals_AllProcessedSuccessfully()
    {
        // Arrange
        string mutexName = $"Test_Mutex_{Guid.NewGuid():N}";
        string pipeName = $"Test_Pipe_{Guid.NewGuid():N}";

        int activationCount = 0;
        using SingleInstanceCoordinator primary = new(
            onActivateRequested: () => Interlocked.Increment(ref activationCount),
            mutexName: mutexName,
            pipeName: pipeName);

        primary.StartListening();

        using SingleInstanceCoordinator secondary = new(
            mutexName: mutexName,
            pipeName: pipeName);

        // Act: signal primary three times
        for (int i = 0; i < 3; i++)
        {
            bool success = secondary.SignalPrimary(timeoutMs: 2000);
            success.Should().BeTrue();
        }

        // Assert
        await Task.Delay(200);
        activationCount.Should().Be(3);
    }

    [Fact]
    public async Task Server_WhenMalformedPayloadReceived_IgnoresAndRemainsListening()
    {
        // Arrange
        string mutexName = $"Test_Mutex_{Guid.NewGuid():N}";
        string pipeName = $"Test_Pipe_{Guid.NewGuid():N}";

        TaskCompletionSource<bool> validActivationTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        using SingleInstanceCoordinator primary = new(
            onActivateRequested: () => validActivationTcs.TrySetResult(true),
            mutexName: mutexName,
            pipeName: pipeName);

        primary.StartListening();

        // Act 1: Send malformed payload directly over pipe
        using (System.IO.Pipes.NamedPipeClientStream rawClient = new(".", pipeName, System.IO.Pipes.PipeDirection.Out))
        {
            await rawClient.ConnectAsync(2000);
            using (System.IO.StreamWriter writer = new(rawClient, leaveOpen: true))
            {
                await writer.WriteLineAsync("UNKNOWN_GARBAGE_PAYLOAD");
                await writer.FlushAsync();
            }
        }

        // Verify valid activation hasn't been triggered yet
        validActivationTcs.Task.IsCompleted.Should().BeFalse();

        // Act 2: Send valid activation signal afterwards
        using SingleInstanceCoordinator secondary = new(
            mutexName: mutexName,
            pipeName: pipeName);

        bool signaled = secondary.SignalPrimary(timeoutMs: 2000);

        // Assert: server survived malformed payload and successfully handled valid activation
        signaled.Should().BeTrue();
        bool activated = await validActivationTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
        activated.Should().BeTrue();
    }

    [Fact]
    public void Dispose_WhileListenerRunning_ShutsDownCleanlyWithoutThrowing()
    {
        // Arrange
        string mutexName = $"Test_Mutex_{Guid.NewGuid():N}";
        string pipeName = $"Test_Pipe_{Guid.NewGuid():N}";

        SingleInstanceCoordinator primary = new(
            mutexName: mutexName,
            pipeName: pipeName);

        primary.StartListening();

        // Act & Assert: disposing while listener is awaiting NamedPipe connection must not throw
        primary.Invoking(p => p.Dispose()).Should().NotThrow();
    }
}
