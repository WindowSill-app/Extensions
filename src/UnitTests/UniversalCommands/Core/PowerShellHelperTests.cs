using WindowSill.UniversalCommands.Core;

namespace UnitTests.UniversalCommands.Core;

public class PowerShellHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task ExecuteAsync_NullOrWhitespace_ReturnsWithoutError(string? command)
    {
        // Should return immediately without launching a process.
        await PowerShellHelper.ExecuteAsync(command);
    }
}
