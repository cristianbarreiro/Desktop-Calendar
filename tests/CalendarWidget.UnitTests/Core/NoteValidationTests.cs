using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Exceptions;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Core;

/// <summary>
/// Unit tests for <see cref="Note.Validate"/> domain invariants.
/// </summary>
public sealed class NoteValidationTests
{
    private static Note ValidNote() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Valid Title",
        Content = "Valid content body",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    // ── Title ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyTitle_ThrowsDomainValidationException()
    {
        Note note = ValidNote();
        note.Title = string.Empty;
        Action act = () => note.Validate();
        act.Should().Throw<DomainValidationException>().WithMessage("*title*");
    }

    [Fact]
    public void Validate_WhitespaceTitleOnly_ThrowsDomainValidationException()
    {
        Note note = ValidNote();
        note.Title = "    ";
        Action act = () => note.Validate();
        act.Should().Throw<DomainValidationException>().WithMessage("*title*");
    }

    [Fact]
    public void Validate_TitleExactly200Chars_Succeeds()
    {
        Note note = ValidNote();
        note.Title = new string('A', 200);
        Action act = () => note.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_TitleOver200Chars_ThrowsDomainValidationException()
    {
        Note note = ValidNote();
        note.Title = new string('A', 201);
        Action act = () => note.Validate();
        act.Should().Throw<DomainValidationException>().WithMessage("*200*");
    }

    // ── Content ───────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyContent_Succeeds()
    {
        Note note = ValidNote();
        note.Content = string.Empty;
        Action act = () => note.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ContentExactly50000Chars_Succeeds()
    {
        Note note = ValidNote();
        note.Content = new string('B', 50_000);
        Action act = () => note.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ContentOver50000Chars_ThrowsDomainValidationException()
    {
        Note note = ValidNote();
        note.Content = new string('B', 50_001);
        Action act = () => note.Validate();
        act.Should().Throw<DomainValidationException>().WithMessage("*50,000*");
    }

    // ── Valid note ────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidNote_DoesNotThrow()
    {
        Note note = ValidNote();
        Action act = () => note.Validate();
        act.Should().NotThrow();
    }
}
