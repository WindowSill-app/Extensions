using FluentAssertions;
using WindowSill.Terminal.Activators;

namespace UnitTests.Terminal.Activators;

public class CommandSelectionActivatorTests
{
    [Theory]
    [InlineData("cmd /c echo hello")]
    [InlineData("cmd.exe /c dir")]
    [InlineData("PS C:\\code> dotnet build")]
    [InlineData("user@host:~/src$ make build")]
    [InlineData("```powershell\nPS C:\\code> Get-Process\n```")]
    public async Task GetShouldBeActivatedAsync_RecognizedCommand_ReturnsTrue(string input)
    {
        // Arrange
        CommandSelectionActivator activator = new();

        // Act
        bool result = await activator.GetShouldBeActivatedAsync(input, isReadOnly: false, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("just some plain text")]
    [InlineData("nonexistent_binary_xyz_12345 --flag")]
    public async Task GetShouldBeActivatedAsync_UnrecognizedText_ReturnsFalse(string input)
    {
        // Arrange
        CommandSelectionActivator activator = new();

        // Act
        bool result = await activator.GetShouldBeActivatedAsync(input, isReadOnly: false, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetShouldBeActivatedAsync_TextTooLong_ReturnsFalse()
    {
        // Arrange
        CommandSelectionActivator activator = new();
        string longText = new('a', 501);

        // Act
        bool result = await activator.GetShouldBeActivatedAsync(longText, isReadOnly: false, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetShouldBeActivatedAsync_NullText_ReturnsFalse()
    {
        // Arrange
        CommandSelectionActivator activator = new();

        // Act
        bool result = await activator.GetShouldBeActivatedAsync(null!, isReadOnly: false, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}
