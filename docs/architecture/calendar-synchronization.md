# Calendar synchronization orchestration

`ICalendarSynchronizationService` is the application-facing orchestration contract. Its enabled-calendar operation discovers connected accounts and enabled external calendars, then runs each calendar in an independent dependency-injection scope. A provider or persistence failure is reported for that calendar and does not stop the remaining calendars.

The per-calendar engine depends only on `ICalendarProvider`, repositories, and the conflict-policy contract. Provider adapters own HTTP, authentication, event mapping, and opaque cursor semantics. The engine loads the calendar cursor and mappings, applies provider additions/updates/deletions, exports eligible local changes, and advances the cursor only after processing succeeds. Event-plus-mapping imports use the repository's atomic save operation. If a run fails after some changes, the old cursor remains so the provider can replay changes; mappings make replay idempotent.

## Offline operations and recovery

Local writes targeting an external calendar are recorded in `PendingCalendarOperations` before remote writes. Failed upserts and deletes remain durable, retain an error for the UI, and are retried by the next explicit Sync Now; no continuous scheduler runs. A failed per-calendar reconciliation rolls back its database transaction and retains the previous cursor, then persists the failure and pending operation outside that transaction. Provider operations must be idempotent or use their stable external identity so replay after a process interruption does not duplicate an event. Delete operations are retained until the local event and its mapping can be consistently removed.

Mappings store a deterministic snapshot hash of the locally synchronized event fields in `LastSyncedLocalVersion`, in addition to the provider's opaque external version. Comparing the current local snapshot with that hash and passing the expected external version to conditional provider writes detects stale remote state without relying only on timestamps. The initial conflict policy remains Local Wins; a provider version precondition failure is reported and not silently retried against the newer version. Corrupt or expired provider cursors are provider errors: the cursor is not advanced, and provider-specific recovery must be explicit rather than interpreting the cursor as a timestamp.

## Initial conflict policy

`LocalWinsCalendarConflictPolicy` is the explicit initial policy. A conflict is a mapped event changed locally since its last successful synchronization while the provider version has also changed (or the provider deleted it). The engine pushes the local event back to the provider, or recreates it after a remote deletion, and reports a `Conflict` outcome rather than silently treating it as a normal update. Remote-only changes continue to flow to the local event.

`ICalendarConflictPolicy` returns a provider-neutral resolution. A future manual policy can return `ManualResolutionRequired`; the engine then leaves both versions and the mapping untouched, reports the conflict, and does not advance that calendar's cursor. Provider implementations do not need to change to add or select another policy.

No background synchronization is registered or started by this service.
