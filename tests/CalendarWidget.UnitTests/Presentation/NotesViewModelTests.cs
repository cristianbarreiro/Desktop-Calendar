using CalendarWidget.Core.Entities;
using CalendarWidget.Presentation.Models;
using CalendarWidget.Presentation.ViewModels;
using CalendarWidget.UnitTests.Fakes;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

/// <summary>
/// Unit tests for <see cref="NotesViewModel"/> covering listing, CRUD, validation, search, selection, and error handling.
/// </summary>
public sealed class NotesViewModelTests
{
    private static (NotesViewModel vm, TestNoteRepository repo) MakeVm()
    {
        TestNoteRepository repo = new();
        TestScopeFactory scopeFactory = new(repo);
        NotesViewModel vm = new(scopeFactory);
        return (vm, repo);
    }

    private static Note MakeNote(string title, string content = "", DateTime? createdAt = null)
    {
        DateTime ts = createdAt ?? DateTime.UtcNow;
        return new Note
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            CreatedAt = ts,
            UpdatedAt = ts,
        };
    }

    // ── Initial loading & Empty State ─────────────────────────────────────────

    [Fact]
    public async Task LoadNotesAsync_EmptyRepository_SetsEmptyState()
    {
        (NotesViewModel vm, _) = MakeVm();
        await vm.RefreshForTestAsync();

        vm.Notes.Should().BeEmpty();
        vm.IsEmpty.Should().BeTrue();
        vm.HasSelectedNote.Should().BeFalse();
        vm.SelectedNote.Should().BeNull();
    }

    [Fact]
    public async Task LoadNotesAsync_PopulatedRepository_LoadsNotesAndSelectsFirst()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        repo.Seed(MakeNote("First Note", "Body 1"));
        repo.Seed(MakeNote("Second Note", "Body 2"));

        await vm.RefreshForTestAsync();

        vm.Notes.Should().HaveCount(2);
        vm.IsEmpty.Should().BeFalse();
        vm.HasSelectedNote.Should().BeTrue();
        vm.SelectedNote!.Title.Should().Be("First Note");
        vm.ViewingTitle.Should().Be("First Note");
        vm.ViewingContent.Should().Be("Body 1");
    }

    // ── Create Note Mode ──────────────────────────────────────────────────────

    [Fact]
    public void NewNote_EntersCreateModeAndClearsEditor()
    {
        (NotesViewModel vm, _) = MakeVm();

        vm.NewNote();

        vm.IsEditing.Should().BeTrue();
        vm.IsCreatingNew.Should().BeTrue();
        vm.EditorHeader.Should().Be("New Note");
        vm.EditorTitle.Should().BeEmpty();
        vm.EditorContent.Should().BeEmpty();
        vm.ValidationError.Should().BeEmpty();
    }

    // ── Edit Note Mode ────────────────────────────────────────────────────────

    [Fact]
    public async Task BeginEdit_WithSelectedNote_PopulatesEditorWithNoteValues()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        repo.Seed(MakeNote("Meeting Notes", "Discuss architecture"));
        await vm.RefreshForTestAsync();

        vm.BeginEdit();

        vm.IsEditing.Should().BeTrue();
        vm.IsCreatingNew.Should().BeFalse();
        vm.EditorHeader.Should().Be("Edit Note");
        vm.EditorTitle.Should().Be("Meeting Notes");
        vm.EditorContent.Should().Be("Discuss architecture");
    }

    [Fact]
    public void BeginEdit_WithNoSelectedNote_DoesNothing()
    {
        (NotesViewModel vm, _) = MakeVm();

        vm.BeginEdit();

        vm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public async Task CancelEdit_RestoresViewingState()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        repo.Seed(MakeNote("Original Title", "Original Content"));
        await vm.RefreshForTestAsync();

        vm.BeginEdit();
        vm.EditorTitle = "Temporary Edit";
        vm.EditorContent = "Temporary Content";

        vm.CancelEdit();

        vm.IsEditing.Should().BeFalse();
        vm.ViewingTitle.Should().Be("Original Title");
        vm.ViewingContent.Should().Be("Original Content");
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveNoteAsync_EmptyTitle_SetsValidationErrorWithoutPersisting()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        vm.NewNote();
        vm.EditorTitle = string.Empty;

        await vm.SaveNoteAsync();

        vm.ValidationError.Should().Contain("title");
        repo.All.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveNoteAsync_WhitespaceTitle_SetsValidationErrorWithoutPersisting()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        vm.NewNote();
        vm.EditorTitle = "     ";

        await vm.SaveNoteAsync();

        vm.ValidationError.Should().Contain("title");
        repo.All.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveNoteAsync_TitleOver200Chars_SetsValidationErrorWithoutPersisting()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        vm.NewNote();
        vm.EditorTitle = new string('A', 201);

        await vm.SaveNoteAsync();

        vm.ValidationError.Should().Contain("200");
        repo.All.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveNoteAsync_ContentOver50000Chars_SetsValidationErrorWithoutPersisting()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        vm.NewNote();
        vm.EditorTitle = "Valid Title";
        vm.EditorContent = new string('B', 50_001);

        await vm.SaveNoteAsync();

        vm.ValidationError.Should().Contain("50,000");
        repo.All.Should().BeEmpty();
    }

    // ── Persistence: Create & Update ──────────────────────────────────────────

    [Fact]
    public async Task SaveNoteAsync_NewNote_PersistsAndSelectsNewNote()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        vm.NewNote();
        vm.EditorTitle = "Shopping List";
        vm.EditorContent = "Apples, Milk, Bread";

        await vm.SaveNoteAsync();

        vm.IsEditing.Should().BeFalse();
        repo.All.Should().HaveCount(1);
        repo.All[0].Title.Should().Be("Shopping List");
        repo.All[0].Content.Should().Be("Apples, Milk, Bread");

        vm.Notes.Should().HaveCount(1);
        vm.SelectedNote.Should().NotBeNull();
        vm.SelectedNote!.Title.Should().Be("Shopping List");
        vm.ViewingTitle.Should().Be("Shopping List");
        vm.ViewingContent.Should().Be("Apples, Milk, Bread");
    }

    [Fact]
    public async Task SaveNoteAsync_EditNote_UpdatesContentPreservingCreatedAt()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        DateTime createdAt = DateTime.UtcNow.AddDays(-2);
        Note initial = MakeNote("Original Title", "Original Content", createdAt);
        repo.Seed(initial);
        await vm.RefreshForTestAsync();

        vm.BeginEdit();
        vm.EditorTitle = "Updated Title";
        vm.EditorContent = "Updated Content";

        await vm.SaveNoteAsync();

        vm.IsEditing.Should().BeFalse();
        Note updated = repo.All[0];
        updated.Title.Should().Be("Updated Title");
        updated.Content.Should().Be("Updated Content");
        updated.CreatedAt.Should().Be(createdAt);
        updated.UpdatedAt.Should().BeAfter(createdAt);

        vm.SelectedNote!.Title.Should().Be("Updated Title");
        vm.ViewingTitle.Should().Be("Updated Title");
        vm.ViewingContent.Should().Be("Updated Content");
    }

    // ── Delete Confirmation & Execution ───────────────────────────────────────

    [Fact]
    public async Task RequestDelete_SetsPendingDeleteAndShowsConfirmationModal()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        Note note = MakeNote("To Delete");
        repo.Seed(note);
        await vm.RefreshForTestAsync();

        vm.RequestDelete();

        vm.IsDeleteConfirmVisible.Should().BeTrue();
        vm.PendingDeleteId.Should().Be(note.Id);
        vm.PendingDeleteTitle.Should().Be("To Delete");
    }

    [Fact]
    public async Task CancelDelete_ClearsPendingStateAndHidesModal()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        repo.Seed(MakeNote("Safe Note"));
        await vm.RefreshForTestAsync();

        vm.RequestDelete();
        vm.CancelDelete();

        vm.IsDeleteConfirmVisible.Should().BeFalse();
        vm.PendingDeleteId.Should().BeEmpty();
        vm.PendingDeleteTitle.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmDeleteAsync_DeletesNoteAndMaintainsAdjacentSelection()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        Note note1 = MakeNote("Note 1");
        Note note2 = MakeNote("Note 2");
        Note note3 = MakeNote("Note 3");
        repo.Seed(note1);
        repo.Seed(note2);
        repo.Seed(note3);
        await vm.RefreshForTestAsync();

        // Select note 2
        vm.SelectedNote = vm.Notes.First(n => n.Id == note2.Id);

        vm.RequestDelete();
        await vm.ConfirmDeleteAsync();

        repo.All.Should().HaveCount(2);
        repo.All.Should().NotContain(n => n.Id == note2.Id);
        vm.Notes.Should().HaveCount(2);

        // Selection should advance to adjacent note (note 3)
        vm.SelectedNote.Should().NotBeNull();
        vm.SelectedNote!.Id.Should().Be(note3.Id);
    }

    [Fact]
    public async Task ConfirmDeleteAsync_LastRemainingNote_SetsEmptyStateAndNullSelection()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        Note note = MakeNote("Only Note");
        repo.Seed(note);
        await vm.RefreshForTestAsync();

        vm.RequestDelete();
        await vm.ConfirmDeleteAsync();

        repo.All.Should().BeEmpty();
        vm.Notes.Should().BeEmpty();
        vm.IsEmpty.Should().BeTrue();
        vm.SelectedNote.Should().BeNull();
        vm.HasSelectedNote.Should().BeFalse();
    }

    // ── Search & Filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task Search_ByTitle_FiltersNoteList()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        repo.Seed(MakeNote("Alpha Meeting"));
        repo.Seed(MakeNote("Beta Planning"));
        repo.Seed(MakeNote("Alpha Retro"));
        await vm.RefreshForTestAsync();

        vm.SearchText = "alpha";
        await vm.RefreshForTestAsync();

        vm.Notes.Should().HaveCount(2);
        vm.Notes.Should().AllSatisfy(n => n.Title.Should().Contain("Alpha"));
    }

    [Fact]
    public async Task Search_ByContent_FiltersNoteList()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        repo.Seed(MakeNote("Document 1", "urgent reminder"));
        repo.Seed(MakeNote("Document 2", "routine checklist"));
        await vm.RefreshForTestAsync();

        vm.SearchText = "urgent";
        await vm.RefreshForTestAsync();

        vm.Notes.Should().HaveCount(1);
        vm.Notes[0].Title.Should().Be("Document 1");
    }

    [Fact]
    public async Task Search_NoMatches_SetsHasNoSearchResults()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        repo.Seed(MakeNote("Some Note"));
        await vm.RefreshForTestAsync();

        vm.SearchText = "nonexistent";
        await vm.RefreshForTestAsync();

        vm.Notes.Should().BeEmpty();
        vm.HasNoSearchResults.Should().BeTrue();
        vm.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ClearSearch_RestoresFullNoteList()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        repo.Seed(MakeNote("Alpha"));
        repo.Seed(MakeNote("Beta"));
        await vm.RefreshForTestAsync();

        vm.SearchText = "alpha";
        await vm.RefreshForTestAsync();
        vm.Notes.Should().HaveCount(1);

        vm.ClearSearch();
        await vm.RefreshForTestAsync();
        vm.Notes.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAndEdit_NoteModifiedToNoLongerMatch_LeavesFilteredList()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        Note note = MakeNote("Alpha Note", "some content");
        repo.Seed(note);
        repo.Seed(MakeNote("Alpha Two", "other content"));
        await vm.RefreshForTestAsync();

        vm.SearchText = "Alpha";
        await vm.RefreshForTestAsync();
        vm.SelectedNote = vm.Notes.First(n => n.Id == note.Id);

        vm.BeginEdit();
        vm.EditorTitle = "Gamma Note"; // No longer contains "Alpha"
        await vm.SaveNoteAsync();

        // Under "Alpha" filter, Gamma Note is no longer included
        vm.Notes.Should().HaveCount(1);
        vm.Notes[0].Title.Should().Be("Alpha Two");
    }

    [Fact]
    public async Task SearchAndDelete_RemovesNoteAndMaintainsSelection()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        Note n1 = MakeNote("Meeting Prep");
        Note n2 = MakeNote("Meeting Minutes");
        repo.Seed(n1);
        repo.Seed(n2);
        await vm.RefreshForTestAsync();

        vm.SearchText = "Meeting";
        await vm.RefreshForTestAsync();
        vm.SelectedNote = vm.Notes.First(n => n.Id == n1.Id);

        vm.RequestDelete();
        await vm.ConfirmDeleteAsync();

        vm.Notes.Should().HaveCount(1);
        vm.Notes[0].Title.Should().Be("Meeting Minutes");
        vm.SelectedNote!.Title.Should().Be("Meeting Minutes");
    }

    // ── Character count & properties ──────────────────────────────────────────

    [Fact]
    public void CharacterCountText_ReflectsEditorContentLength()
    {
        (NotesViewModel vm, _) = MakeVm();
        vm.NewNote();
        vm.EditorContent = "12345";

        vm.CharacterCountText.Should().Be("5 / 50,000 characters");
    }

    [Fact]
    public async Task OnSelectedNoteChanged_UpdatesViewingProperties()
    {
        (NotesViewModel vm, TestNoteRepository repo) = MakeVm();
        Note n1 = MakeNote("Note A", "Content A");
        Note n2 = MakeNote("Note B", "Content B");
        repo.Seed(n1);
        repo.Seed(n2);
        await vm.RefreshForTestAsync();

        vm.SelectedNote = vm.Notes.First(n => n.Id == n2.Id);

        vm.ViewingTitle.Should().Be("Note B");
        vm.ViewingContent.Should().Be("Content B");
        vm.ViewingCreatedAtFormatted.Should().NotBeEmpty();
        vm.ViewingUpdatedAtFormatted.Should().NotBeEmpty();
    }
}
