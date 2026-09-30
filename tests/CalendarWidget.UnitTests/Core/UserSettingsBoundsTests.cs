using CalendarWidget.Core.Entities;
using CalendarWidget.Core.Exceptions;
using FluentAssertions;

namespace CalendarWidget.UnitTests.Core;

public sealed class UserSettingsBoundsTests
{
    [Fact]
    public void Validate_WithValidBoundsAndTraySetting_Succeeds()
    {
        // Arrange
        UserSettings settings = new()
        {
            MinimizeToTray = true,
            MainWindowLeft = 100,
            MainWindowTop = 150,
            MainWindowWidth = 960,
            MainWindowHeight = 620,
            WidgetWindowLeft = 200,
            WidgetWindowTop = 300,
        };

        // Act
        Action act = () => settings.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(199.0)]
    [InlineData(0.0)]
    [InlineData(-100.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithInvalidMainWindowWidth_ThrowsDomainValidationException(double invalidWidth)
    {
        // Arrange
        UserSettings settings = new()
        {
            MainWindowWidth = invalidWidth,
        };

        // Act
        Action act = () => settings.Validate();

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*MainWindowWidth*");
    }

    [Theory]
    [InlineData(199.0)]
    [InlineData(0.0)]
    [InlineData(-50.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithInvalidMainWindowHeight_ThrowsDomainValidationException(double invalidHeight)
    {
        // Arrange
        UserSettings settings = new()
        {
            MainWindowHeight = invalidHeight,
        };

        // Act
        Action act = () => settings.Validate();

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*MainWindowHeight*");
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithNonFiniteCoordinates_ThrowsDomainValidationException(double invalidCoord)
    {
        // Arrange
        UserSettings mainLeft = new() { MainWindowLeft = invalidCoord };
        UserSettings mainTop = new() { MainWindowTop = invalidCoord };
        UserSettings widgetLeft = new() { WidgetWindowLeft = invalidCoord };
        UserSettings widgetTop = new() { WidgetWindowTop = invalidCoord };

        // Act & Assert
        mainLeft.Invoking(s => s.Validate()).Should().Throw<DomainValidationException>();
        mainTop.Invoking(s => s.Validate()).Should().Throw<DomainValidationException>();
        widgetLeft.Invoking(s => s.Validate()).Should().Throw<DomainValidationException>();
        widgetTop.Invoking(s => s.Validate()).Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Clone_CopiesAllBoundsAndTrayPropertiesFaithfully()
    {
        // Arrange
        UserSettings original = new()
        {
            MinimizeToTray = false,
            MainWindowLeft = -1500,
            MainWindowTop = 120,
            MainWindowWidth = 1024,
            MainWindowHeight = 768,
            WidgetWindowLeft = -500,
            WidgetWindowTop = 40,
        };

        // Act
        UserSettings cloned = original.Clone();

        // Assert
        cloned.MinimizeToTray.Should().BeFalse();
        cloned.MainWindowLeft.Should().Be(-1500);
        cloned.MainWindowTop.Should().Be(120);
        cloned.MainWindowWidth.Should().Be(1024);
        cloned.MainWindowHeight.Should().Be(768);
        cloned.WidgetWindowLeft.Should().Be(-500);
        cloned.WidgetWindowTop.Should().Be(40);
    }
}
