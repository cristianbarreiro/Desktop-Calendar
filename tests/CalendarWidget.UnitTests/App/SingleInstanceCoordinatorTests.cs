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
}
