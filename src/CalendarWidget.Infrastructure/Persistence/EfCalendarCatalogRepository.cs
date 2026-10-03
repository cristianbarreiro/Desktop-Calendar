using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CalendarWidget.Infrastructure.Persistence;

public sealed class EfCalendarCatalogRepository(AppDbContext context) : ICalendarCatalogRepository
{
    public async Task<IReadOnlyList<CalendarAccount>> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        await context.CalendarAccounts.AsNoTracking().OrderBy(account => account.DisplayName).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Calendar>> GetCalendarsAsync(CancellationToken cancellationToken = default) =>
        await context.Calendars.AsNoTracking().OrderBy(calendar => calendar.Name).ToListAsync(cancellationToken);

    public async Task SaveAccountAsync(CalendarAccount account, CancellationToken cancellationToken = default)
    {
        account.Validate();
        CalendarAccount? current = await context.CalendarAccounts
            .FirstOrDefaultAsync(existing => existing.Id == account.Id, cancellationToken);
        if (current is null)
            context.CalendarAccounts.Add(account);
        else
            context.Entry(current).CurrentValues.SetValues(account);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveCalendarAsync(Calendar calendar, CancellationToken cancellationToken = default)
    {
        calendar.Validate();
        if (calendar.AccountId is Guid accountId)
        {
            CalendarAccount? account = await context.CalendarAccounts.AsNoTracking()
                .FirstOrDefaultAsync(existing => existing.Id == accountId, cancellationToken);
            if (account is null || account.Provider != calendar.Provider)
                throw new DomainValidationException("Calendar provider must match its account provider.");
        }

        Calendar? current = await context.Calendars
            .FirstOrDefaultAsync(existing => existing.Id == calendar.Id, cancellationToken);
        if (current is null)
            context.Calendars.Add(calendar);
        else
            context.Entry(current).CurrentValues.SetValues(calendar);
        await context.SaveChangesAsync(cancellationToken);
    }
}
