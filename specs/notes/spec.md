# Notes Feature Specification

## 1. Overview
The Notes feature provides local-first, lightweight text capture inside the Full Desktop Application. It enables users to record quick thoughts, checklists, or date-independent memos.

---

## 2. Behavioral Specifications

### 2.1 Note Creation
- **Trigger**: User clicks "New Note" action or invokes keyboard shortcut (`Ctrl+N` while in Notes view).
- **State Changes**:
  - A new `Note` instance is created with an auto-generated GUID, empty title placeholder, empty content, and `CreatedAt` / `UpdatedAt` assigned to current UTC time.
  - Active editor shifts focus to the title field.
- **Constraints**:
  - Title cannot be empty when saved.
  - Maximum title length: 200 characters.
  - Maximum content length: 50,000 characters.
- **Verification / Test Scenarios**:
  - Creating a note adds it to the internal collection and sets `SelectedNote`.
  - Saving a note without a title is rejected with validation message.

### 2.2 Note Editing & Persistence
- **Trigger**: User types changes in the Title or Content fields.
- **State Changes**:
  - `UpdatedAt` timestamp refreshes to current UTC time.
  - Changes are persisted locally via `INoteRepository.UpdateAsync(note)`.
- **Verification / Test Scenarios**:
  - Given an existing note, editing content updates the `UpdatedAt` timestamp to a value greater than `CreatedAt`.

### 2.3 Note Deletion
- **Trigger**: User clicks "Delete Note" action (`Delete` key or icon).
- **State Changes**:
  - Confirmation prompt is presented.
  - Upon confirmation, record is deleted from SQLite via `INoteRepository.DeleteAsync(id)`.
  - `SelectedNote` shifts to the adjacent note or `null`.
- **Verification / Test Scenarios**:
  - Deleting note ID removes it from the database and updates note count by -1.

### 2.4 Note Search
- **Trigger**: User types text into the notes search input.
- **State Changes**:
  - The displayed note list filters to items where Title or Content contains the search query (case-insensitive).
  - Empty search query restores the complete note list.
- **Verification / Test Scenarios**:
  - Given notes ["Project Alpha", "Grocery List", "Alpha Study"], query "alpha" returns exactly 2 notes.
