using CalendarWidget.Presentation.ViewModels;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Presentation;

public sealed class EventFormViewModelTests
{
    private static EventFormViewModel ValidForm() => new()
    {
        Title = "Team Meeting",
        Description = string.Empty,
        StartTime = new DateTime(2026, 9, 15, 10, 0, 0),
        EndTime = new DateTime(2026, 9, 15, 11, 0, 0),
        IsAllDay = false,
    };

    [Fact]
    public void IsValid_EmptyTitle_ReturnsFalseAndSetsError()
    {
        EventFormViewModel form = ValidForm();
        form.Title = string.Empty;
        form.IsValid().Should().BeFalse();
        form.ValidationError.Should().NotBeEmpty();
    }

    [Fact]
    public void IsValid_WhitespaceTitle_ReturnsFalseAndSetsError()
    {
        EventFormViewModel form = ValidForm();
        form.Title = "   ";
        form.IsValid().Should().BeFalse();
        form.ValidationError.Should().NotBeEmpty();
    }

    [Fact]
    public void IsValid_TitleExactly200Chars_ReturnsTrue()
    {
        EventFormViewModel form = ValidForm();
        form.Title = new string('A', 200);
        form.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_TitleOver200Chars_ReturnsFalseAndSetsError()
    {
        EventFormViewModel form = ValidForm();
        form.Title = new string('A', 201);
        form.IsValid().Should().BeFalse();
        form.ValidationError.Should().Contain("200");
    }

    [Fact]
    public void IsValid_DescriptionExactly2000Chars_ReturnsTrue()
    {
        EventFormViewModel form = ValidForm();
        form.Description = new string('B', 2000);
        form.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_DescriptionOver2000Chars_ReturnsFalseAndSetsError()
    {
        EventFormViewModel form = ValidForm();
        form.Description = new string('B', 2001);
        form.IsValid().Should().BeFalse();
        form.ValidationError.Should().Contain("2000");
    }

    [Fact]
    public void IsValid_EndTimeEqualToStartTime_ReturnsTrue()
    {
        EventFormViewModel form = ValidForm();
        form.EndTime = form.StartTime;
        form.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_EndTimeBeforeStartTime_ReturnsFalseAndSetsError()
    {
        EventFormViewModel form = ValidForm();
        form.EndTime = form.StartTime.AddMinutes(-1);
        form.IsValid().Should().BeFalse();
        form.ValidationError.Should().NotBeEmpty();
    }

    [Fact]
    public void IsValid_ValidForm_ReturnsTrueAndClearsError()
    {
        EventFormViewModel form = ValidForm();
        form.IsValid().Should().BeTrue();
        form.ValidationError.Should().BeEmpty();
    }

    [Fact]
    public void IsEditing_WithEditingId_ReturnsTrue()
    {
        EventFormViewModel form = ValidForm();
        form.EditingId = Guid.NewGuid();
        form.IsEditing.Should().BeTrue();
    }

    [Fact]
    public void IsEditing_WithoutEditingId_ReturnsFalse()
    {
        EventFormViewModel form = ValidForm();
        form.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void ClearError_ResetsValidationError()
    {
        EventFormViewModel form = ValidForm();
        form.Title = string.Empty;
        form.IsValid();
        form.ValidationError.Should().NotBeEmpty();
        form.ClearError();
        form.ValidationError.Should().BeEmpty();
    }

    [Fact]
    public void FormTitle_WhenNewEvent_ReturnsNewEvent()
    {
        EventFormViewModel form = ValidForm();
        form.EditingId = null;
        form.FormTitle.Should().Be("New Event");
    }

    [Fact]
    public void FormTitle_WhenEditing_ReturnsEditEvent()
    {
        EventFormViewModel form = ValidForm();
        form.EditingId = Guid.NewGuid();
        form.FormTitle.Should().Be("Edit Event");
    }

    [Fact]
    public void IsValid_AllDayEvent_DoesNotValidateEndTimeBeforeStartTime()
    {
        EventFormViewModel form = ValidForm();
        form.IsAllDay = true;
        form.EndTime = form.StartTime.AddHours(-2);
        form.IsValid().Should().BeTrue();
    }
}
