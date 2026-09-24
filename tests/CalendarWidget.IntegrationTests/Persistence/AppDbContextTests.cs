using CalendarWidget.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CalendarWidget.IntegrationTests.Persistence;

/// <summary>
/// Validates database context creation and basic persistence operations.
/// </summary>
public sealed class AppDbContextTests
{
    [Fact]
    public async Task Database_CanBeCreated_WithInMemoryProvider()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        bool created = await context.Database.EnsureCreatedAsync();
        created.Should().BeTrue();
    }
}
