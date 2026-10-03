using CalendarWidget.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CalendarWidget.Infrastructure.Persistence;

/// <summary>
/// Application database context for SQLite persistence.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="AppDbContext"/>.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>Gets the calendar events set.</summary>
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();

    public DbSet<Calendar> Calendars => Set<Calendar>();

    public DbSet<CalendarAccount> CalendarAccounts => Set<CalendarAccount>();

    public DbSet<CalendarEventMapping> CalendarEventMappings => Set<CalendarEventMapping>();

    public DbSet<CalendarSyncState> CalendarSyncStates => Set<CalendarSyncState>();

    public DbSet<PendingCalendarOperation> PendingCalendarOperations => Set<PendingCalendarOperation>();

    /// <summary>Gets the notes set.</summary>
    public DbSet<Note> Notes => Set<Note>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.CalendarId).IsRequired().HasDefaultValue(CalendarIdentity.LocalCalendarId);
            entity.HasOne<Calendar>()
                .WithMany()
                .HasForeignKey(e => e.CalendarId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.StartTime, e.EndTime });
        });

        modelBuilder.Entity<CalendarAccount>(entity =>
        {
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Provider).HasConversion<string>().HasMaxLength(32);
            entity.Property(account => account.ProviderAccountId).IsRequired().HasMaxLength(500);
            entity.Property(account => account.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(account => account.IsConnected).IsRequired();
            entity.HasIndex(account => new { account.Provider, account.ProviderAccountId }).IsUnique();
        });

        modelBuilder.Entity<Calendar>(entity =>
        {
            entity.HasKey(calendar => calendar.Id);
            entity.Property(calendar => calendar.Provider).HasConversion<string>().HasMaxLength(32);
            entity.Property(calendar => calendar.Name).IsRequired().HasMaxLength(200);
            entity.Property(calendar => calendar.ExternalId).IsRequired().HasMaxLength(500);
            entity.Property(calendar => calendar.IsEnabled).IsRequired();
            entity.HasOne<CalendarAccount>()
                .WithMany()
                .HasForeignKey(calendar => calendar.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(calendar => new { calendar.Provider, calendar.AccountId, calendar.ExternalId }).IsUnique();
            entity.HasData(new Calendar
            {
                Id = CalendarIdentity.LocalCalendarId,
                Provider = CalendarWidget.Core.Enums.CalendarProvider.Local,
                Name = "Local Calendar",
                ExternalId = "local",
                IsEnabled = true,
                CreatedAt = DateTime.UnixEpoch
            });
        });

        modelBuilder.Entity<CalendarEventMapping>(entity =>
        {
            entity.HasKey(mapping => new { mapping.InternalEventId, mapping.Provider, mapping.AccountId, mapping.CalendarId });
            entity.Property(mapping => mapping.Provider).HasConversion<string>().HasMaxLength(32);
            entity.Property(mapping => mapping.ExternalEventId).IsRequired().HasMaxLength(500);
            entity.Property(mapping => mapping.ExternalVersion).HasMaxLength(500);
            entity.Property(mapping => mapping.LastSyncedLocalVersion).HasMaxLength(64);
            entity.HasOne<CalendarEvent>()
                .WithMany()
                .HasForeignKey(mapping => mapping.InternalEventId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CalendarAccount>()
                .WithMany()
                .HasForeignKey(mapping => mapping.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Calendar>()
                .WithMany()
                .HasForeignKey(mapping => mapping.CalendarId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(mapping => new { mapping.Provider, mapping.AccountId, mapping.CalendarId, mapping.ExternalEventId })
                .IsUnique();
        });

        modelBuilder.Entity<CalendarSyncState>(entity =>
        {
            entity.HasKey(state => state.CalendarId);
            entity.Property(state => state.Cursor);
            entity.Property(state => state.LastError).HasMaxLength(2000);
            entity.HasOne<Calendar>()
                .WithMany()
                .HasForeignKey(state => state.CalendarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PendingCalendarOperation>(entity =>
        {
            entity.HasKey(operation => operation.Id);
            entity.Property(operation => operation.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(operation => operation.LastError).HasMaxLength(2000);
            entity.HasOne<Calendar>().WithMany().HasForeignKey(operation => operation.CalendarId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(operation => new { operation.CalendarId, operation.InternalEventId, operation.Type }).IsUnique();
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).HasMaxLength(50_000);
        });
    }
}
