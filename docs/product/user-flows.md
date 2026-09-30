# User Flows

## Widget Flows

### View Calendar (Default)
1. User sees widget on desktop
2. Widget shows current month, time, weekday headers, calendar grid
3. Current day is visually indicated

### Select a Day
1. User clicks a day in the calendar grid
2. Widget expands vertically to show day detail
3. Day detail shows events for that day (if any)
4. Clicking the same day or another day updates the detail

### Open Full Application
1. User clicks the [APP] button in the widget
2. Full application window opens
3. Widget may minimize or remain visible (configurable)

### Minimize Widget
1. User clicks the [─] control
2. Widget minimizes (behavior TBD: system tray or taskbar)

## Full Application Flows

### Navigate Calendar
1. User sees current month in calendar view
2. User clicks previous/next arrows to navigate months
3. Calendar updates to show the selected month

### Create Event
1. User selects a day in the calendar
2. User clicks "Add Event" or uses keyboard shortcut
3. Event creation form appears
4. User enters title, optional description, start/end time
5. User saves → event persisted to SQLite
6. Calendar updates to show event indicator

### Edit Event
1. User selects a day with events
2. User clicks an existing event
3. Event edit form appears with pre-filled data
4. User modifies fields and saves

### Delete Event
1. User opens an existing event
2. User clicks delete action
3. Confirmation dialog appears
4. Event is removed from database

### Manage Notes
1. User navigates to Notes section
2. User sees list of existing notes
3. User can create, edit, or delete notes
4. Notes are persisted locally

### Change Settings
1. User navigates to Settings section
2. User selects theme (Dark / Light / System) → theme updates instantly across all windows without restart
3. User adjusts widget opacity slider (50%–100%) → widget opacity updates in real time
4. User toggles "Always on top" → widget window topmost behavior updates immediately
5. User toggles "Start with Windows" → updates user registry run key without administrator prompts
6. User modifies first day of week (Monday / Sunday) or time/date format → calendar and time displays update dynamically
7. Settings persist automatically to `%LocalAppData%\DesktopCalendar\settings.json`

### Export Application Data
1. User navigates to Settings → Data Management
2. User clicks "Export Backup"
3. Save file dialog appears with timestamped default filename
4. User chooses destination and confirms
5. Events, notes, and preferences are serialized to JSON backup file
6. Success feedback banner displays confirmation

### Import Application Data
1. User navigates to Settings → Data Management
2. User clicks "Import Backup"
3. Open file dialog appears
4. User selects valid JSON backup file
5. System validates backup structure, schema, and domain rules atomically
6. Valid new events and notes are imported transactionally into SQLite database; existing IDs are skipped to avoid overwriting
7. Calendar and Notes views refresh with updated data, and success banner displays counts of imported/skipped records

### Reset Database
1. User navigates to Settings → Data Management
2. User clicks "Reset All Data"
3. Destructive confirmation modal appears warning that all events and notes will be permanently removed
4. User confirms reset
5. All user events and notes are deleted from the database while keeping schema and preferences intact
6. Views refresh and success banner appears

### Switch to Widget
1. User clicks [WIDGET] button in the full application
2. Full application closes or minimizes
3. Widget appears on desktop
