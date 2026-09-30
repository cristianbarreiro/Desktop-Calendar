using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CalendarWidget.IntegrationTests.Helpers;

/// <summary>
/// Interceptor for injecting faults into <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
/// </summary>
public sealed class TestSaveChangesInterceptor : SaveChangesInterceptor
{
    public bool FailSavingChanges { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (FailSavingChanges)
        {
            throw new DbUpdateException("Simulated database persistence failure.", (Exception?)null);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

/// <summary>
/// Interceptor for injecting faults into database transaction commit and rollback workflows.
/// </summary>
public sealed class TestTransactionInterceptor : DbTransactionInterceptor
{
    public bool FailCommit { get; set; }
    public bool FailRollback { get; set; }

    public override ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (FailCommit)
        {
            throw new InvalidOperationException("Simulated transaction commit failure.");
        }

        return base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult> TransactionRollingBackAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (FailRollback)
        {
            throw new InvalidOperationException("Simulated transaction rollback failure.");
        }

        return base.TransactionRollingBackAsync(transaction, eventData, result, cancellationToken);
    }
}
