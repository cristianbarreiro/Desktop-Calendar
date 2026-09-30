using System.Globalization;
using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Exceptions;
using CalendarWidget.Core.Interfaces;
using CalendarWidget.Presentation.Models;
using CalendarWidget.Presentation.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarWidget.Presentation.ViewModels;

/// <summary>
/// ViewModel for the main application notes view.
/// Handles listing, creating, editing, deleting, searching, and validating notes.
/// </summary>
public sealed partial class NotesViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IClockService? _clockService;
    private readonly IDataManagementService? _dataManagementService;

    // ── Header & Basic Info ───────────────────────────────────────────────────

    [ObservableProperty]
    private string _title = "Notes";

    [ObservableProperty]
    private string _emptyStateTitle = "No Notes Yet";

    [ObservableProperty]
    private string _emptyStateDescription = "Capture quick thoughts, checklists, or memos by creating your first note.";

    // ── Notes Collection & Selection ──────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoSearchResults))]
    private IReadOnlyList<NoteListItemModel> _notes = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedNote))]
    private NoteListItemModel? _selectedNote;

    /// <summary>Gets whether a note is currently selected.</summary>
    public bool HasSelectedNote => SelectedNote is not null;

    // ── Search State ──────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearching))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoSearchResults))]
    private string _searchText = string.Empty;

    /// <summary>Gets whether an active search query is applied.</summary>
    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>Gets whether there are no notes in the repository (and not searching).</summary>
    public bool IsEmpty => !IsSearching && Notes.Count == 0 && !IsLoading;

    /// <summary>Gets whether a search yielded zero matching results.</summary>
    public bool HasNoSearchResults => IsSearching && Notes.Count == 0 && !IsLoading;

    // ── Viewing State (Read-only Detail) ──────────────────────────────────────

    [ObservableProperty]
    private string _viewingTitle = string.Empty;

    [ObservableProperty]
    private string _viewingContent = string.Empty;

    [ObservableProperty]
    private string _viewingCreatedAtFormatted = string.Empty;

    [ObservableProperty]
    private string _viewingUpdatedAtFormatted = string.Empty;

    // ── Editor / Form State ───────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isCreatingNew;

    [ObservableProperty]
    private string _editorHeader = "New Note";

    [ObservableProperty]
    private string _editorTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CharacterCountText))]
    private string _editorContent = string.Empty;

    [ObservableProperty]
    private string _validationError = string.Empty;

    /// <summary>Gets formatted character count for the note content editor.</summary>
    public string CharacterCountText => $"{EditorContent?.Length ?? 0:N0} / 50,000 characters";

    // ── Delete Confirmation ───────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isDeleteConfirmVisible;

    [ObservableProperty]
    private Guid _pendingDeleteId;

    [ObservableProperty]
    private string _pendingDeleteTitle = string.Empty;

    // ── Loading & Error States ────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(HasNoSearchResults))]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isDeleting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    /// <summary>Gets whether an error message is active.</summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of the <see cref="NotesViewModel"/> class for design-time and fallback contexts.
    /// </summary>
    public NotesViewModel() : this(null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotesViewModel"/> class with the specified dependencies.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating service scopes to resolve repositories.</param>
    /// <param name="clockService">Clock service for timestamp generation.</param>
    public NotesViewModel(IServiceScopeFactory? scopeFactory, IClockService? clockService = null)
        : this(scopeFactory, clockService, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotesViewModel"/> class with data management support.
    /// </summary>
    public NotesViewModel(
        IServiceScopeFactory? scopeFactory,
        IClockService? clockService,
        IDataManagementService? dataManagementService)
    {
        _scopeFactory = scopeFactory;
        _clockService = clockService;
        _dataManagementService = dataManagementService;

        if (_dataManagementService is not null)
        {
            _dataManagementService.DataChanged += OnDataChanged;
        }

        if (_scopeFactory is not null)
        {
            _ = LoadNotesAsync();
        }
    }

    // ── Change Handlers ───────────────────────────────────────────────────────

    partial void OnSelectedNoteChanged(NoteListItemModel? value)
    {
        if (IsEditing)
            return;

        if (value is not null)
        {
            ViewingTitle = value.Title;
            ViewingContent = value.Content;

            DateTime localCreated = value.CreatedAt.Kind == DateTimeKind.Utc
                ? value.CreatedAt.ToLocalTime()
                : value.CreatedAt;
            DateTime localUpdated = value.UpdatedAt.Kind == DateTimeKind.Utc
                ? value.UpdatedAt.ToLocalTime()
                : value.UpdatedAt;

            ViewingCreatedAtFormatted = localCreated.ToString("g", CultureInfo.CurrentCulture);
            ViewingUpdatedAtFormatted = localUpdated.ToString("g", CultureInfo.CurrentCulture);
        }
        else
        {
            ViewingTitle = string.Empty;
            ViewingContent = string.Empty;
            ViewingCreatedAtFormatted = string.Empty;
            ViewingUpdatedAtFormatted = string.Empty;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_scopeFactory is not null)
        {
            _ = LoadNotesAsync();
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>Loads or refreshes the notes collection from persistence.</summary>
    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        if (_scopeFactory is null)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            INoteRepository repo = scope.ServiceProvider.GetRequiredService<INoteRepository>();

            IReadOnlyList<Note> entities = string.IsNullOrWhiteSpace(SearchText)
                ? await repo.GetAllAsync()
                : await repo.SearchAsync(SearchText.Trim());

            Notes = entities.Select(NoteListItemModel.FromEntity).ToList();

            // Maintain or restore selection
            if (SelectedNote is not null)
            {
                NoteListItemModel? existingInList = Notes.FirstOrDefault(n => n.Id == SelectedNote.Id);
                SelectedNote = existingInList ?? (Notes.Count > 0 ? Notes[0] : null);
            }
            else if (Notes.Count > 0 && !IsEditing)
            {
                SelectedNote = Notes[0];
            }
            else
            {
                SelectedNote = null;
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Failed to load notes. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Starts creating a new note.</summary>
    [RelayCommand]
    public void NewNote()
    {
        IsCreatingNew = true;
        IsEditing = true;
        EditorHeader = "New Note";
        EditorTitle = string.Empty;
        EditorContent = string.Empty;
        ValidationError = string.Empty;
    }

    /// <summary>Starts editing the currently selected note.</summary>
    [RelayCommand]
    public void BeginEdit()
    {
        if (SelectedNote is null)
            return;

        IsCreatingNew = false;
        IsEditing = true;
        EditorHeader = "Edit Note";
        EditorTitle = SelectedNote.Title;
        EditorContent = SelectedNote.Content;
        ValidationError = string.Empty;
    }

    /// <summary>Cancels the current create or edit operation.</summary>
    [RelayCommand]
    public void CancelEdit()
    {
        IsEditing = false;
        IsCreatingNew = false;
        ValidationError = string.Empty;

        // Restore viewing details from selected note
        OnSelectedNoteChanged(SelectedNote);
    }

    /// <summary>Validates and saves the current note to persistence.</summary>
    [RelayCommand]
    public async Task SaveNoteAsync()
    {
        ValidationError = string.Empty;

        if (string.IsNullOrWhiteSpace(EditorTitle))
        {
            ValidationError = "Note title is required.";
            return;
        }

        if (EditorTitle.Trim().Length > 200)
        {
            ValidationError = "Note title cannot exceed 200 characters.";
            return;
        }

        if (EditorContent is not null && EditorContent.Length > 50_000)
        {
            ValidationError = "Note content cannot exceed 50,000 characters.";
            return;
        }

        if (_scopeFactory is null)
            return;

        IsSaving = true;

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            INoteRepository repo = scope.ServiceProvider.GetRequiredService<INoteRepository>();

            DateTime nowUtc = _clockService?.Now.ToUniversalTime() ?? DateTime.UtcNow;

            if (IsCreatingNew)
            {
                Note newNote = new()
                {
                    Id = Guid.NewGuid(),
                    Title = EditorTitle.Trim(),
                    Content = EditorContent ?? string.Empty,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc,
                };

                newNote.Validate();
                await repo.AddAsync(newNote);

                IsEditing = false;
                IsCreatingNew = false;

                await LoadNotesAsync();
                SelectedNote = Notes.FirstOrDefault(n => n.Id == newNote.Id);
            }
            else
            {
                if (SelectedNote is null)
                    return;

                Note? existing = await repo.GetByIdAsync(SelectedNote.Id);
                if (existing is null)
                {
                    ValidationError = "The note being edited was not found.";
                    return;
                }

                existing.Title = EditorTitle.Trim();
                existing.Content = EditorContent ?? string.Empty;
                existing.UpdatedAt = nowUtc; // CreatedAt is strictly preserved

                existing.Validate();
                await repo.UpdateAsync(existing);

                IsEditing = false;
                IsCreatingNew = false;

                await LoadNotesAsync();
                SelectedNote = Notes.FirstOrDefault(n => n.Id == existing.Id);
            }
        }
        catch (DomainValidationException ex)
        {
            ValidationError = ex.Message;
        }
        catch (Exception)
        {
            ValidationError = "Failed to save note. Please try again.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Opens the delete confirmation modal for the selected note.</summary>
    [RelayCommand]
    public void RequestDelete()
    {
        if (SelectedNote is null)
            return;

        PendingDeleteId = SelectedNote.Id;
        PendingDeleteTitle = SelectedNote.Title;
        IsDeleteConfirmVisible = true;
    }

    /// <summary>Cancels the delete confirmation modal.</summary>
    [RelayCommand]
    public void CancelDelete()
    {
        IsDeleteConfirmVisible = false;
        PendingDeleteId = Guid.Empty;
        PendingDeleteTitle = string.Empty;
    }

    /// <summary>Confirms deletion and removes the note from persistence.</summary>
    [RelayCommand]
    public async Task ConfirmDeleteAsync()
    {
        Guid id = PendingDeleteId;
        IsDeleteConfirmVisible = false;
        PendingDeleteId = Guid.Empty;
        PendingDeleteTitle = string.Empty;

        if (id == Guid.Empty || _scopeFactory is null)
            return;

        IsDeleting = true;

        try
        {
            // Determine adjacent item to maintain selection consistency
            int currentIndex = -1;
            for (int i = 0; i < Notes.Count; i++)
            {
                if (Notes[i].Id == id)
                {
                    currentIndex = i;
                    break;
                }
            }

            Guid? nextSelectedId = null;
            if (currentIndex >= 0)
            {
                if (currentIndex + 1 < Notes.Count)
                    nextSelectedId = Notes[currentIndex + 1].Id;
                else if (currentIndex - 1 >= 0)
                    nextSelectedId = Notes[currentIndex - 1].Id;
            }

            using IServiceScope scope = _scopeFactory.CreateScope();
            INoteRepository repo = scope.ServiceProvider.GetRequiredService<INoteRepository>();

            await repo.DeleteAsync(id);
            await LoadNotesAsync();

            if (nextSelectedId.HasValue)
            {
                SelectedNote = Notes.FirstOrDefault(n => n.Id == nextSelectedId.Value);
            }
            else
            {
                SelectedNote = Notes.Count > 0 ? Notes[0] : null;
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Failed to delete note. Please try again.";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    /// <summary>Clears the search input and reloads all notes.</summary>
    [RelayCommand]
    public void ClearSearch()
    {
        SearchText = string.Empty;
    }

    /// <summary>Dismisses the current error banner.</summary>
    [RelayCommand]
    public void DismissError()
    {
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Explicit helper for testing to await note loading without relying on background constructors.
    /// </summary>
    public async Task RefreshForTestAsync()
    {
        await LoadNotesAsync();
    }

    private void OnDataChanged(object? sender, EventArgs e)
    {
        _ = LoadNotesAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_dataManagementService is not null)
        {
            _dataManagementService.DataChanged -= OnDataChanged;
        }
    }
}
