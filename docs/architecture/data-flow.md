# Data Flow

## Unidirectional Flow Architecture

The application enforces a predictable unidirectional data flow across all UI interactions:

```
[ User Interaction (WPF View) ]
              │
              ▼
    [ ICommand / Event ]
              │
              ▼
      [ ViewModel (State) ]
              │
              ▼
   [ Domain / Interface Call ]
              │
              ▼
   [ Infrastructure / Repository ]
              │
              ▼
      [ SQLite Database ]
              │
   (Return Data / Task Result)
              ▼
      [ ViewModel Updates ]
              │
              ▼
  [ INotifyPropertyChanged / UI Update ]
```

## Key Scenarios

### 1. Day Selection in Widget Mode
1. User clicks a date in `WidgetView`.
2. `SelectedDateCommand` triggers on `WidgetViewModel`.
3. `WidgetViewModel` updates its `SelectedDate` property.
4. `WidgetViewModel` invokes `ICalendarEventRepository.GetByDateRangeAsync(...)` for that specific day.
5. Events collection updates; `IsExpanded` is set to `true`.
6. WPF DataBinding updates the day details tray smoothly.

### 2. Event Creation in Full Application Mode
1. User inputs event details and submits in `CalendarView`.
2. `CreateEventCommand` triggers on `CalendarViewModel`.
3. Input validation occurs (Title length, Date ranges).
4. `CalendarViewModel` constructs `CalendarEvent` domain entity.
5. Invokes `ICalendarEventRepository.AddAsync(event)`.
6. Database persists record inside a transaction.
7. On success, `CalendarViewModel` updates observable collection.
8. Day cell displays the event dot indicator.

### 3. Concurrency & Threading
- Database I/O is asynchronous using `async/await` with `CancellationToken`.
- UI updates are guaranteed on the WPF Dispatcher / UI Thread.
- Heavy background computations (e.g., recurrence expansion) are run off the UI thread via `Task.Run` and marshaled back.
