using FluentAssertions;
using WindowSill.Terminal.Core.Shell;

namespace UnitTests.Terminal.Core;

public class ShellDetectionServiceTests
{
    [Fact]
    public async Task GetAvailableShells_ReturnsAtLeastOneShellAsync()
    {
        // Arrange
        ShellDetectionService service = new();

        // Act
        IReadOnlyList<ShellInfo> shells = await service.GetAvailableShellsAsync();

        // Assert – CI and dev machines always have at least cmd.
        shells.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAvailableShells_AlwaysIncludesCommandPromptAsync()
    {
        // Arrange
        ShellDetectionService service = new();

        // Act
        IReadOnlyList<ShellInfo> shells = await service.GetAvailableShellsAsync();

        // Assert
        shells.Should().Contain(s => s.DisplayName == "Command Prompt");
    }

    [Fact]
    public async Task GetAvailableShells_ShellsHaveValidPropertiesAsync()
    {
        // Arrange
        ShellDetectionService service = new();

        // Act
        IReadOnlyList<ShellInfo> shells = await service.GetAvailableShellsAsync();

        // Assert
        foreach (ShellInfo shell in shells)
        {
            shell.DisplayName.Should().NotBeNullOrWhiteSpace();
            shell.ExecutablePath.Should().NotBeNullOrWhiteSpace();
            File.Exists(shell.ExecutablePath).Should().BeTrue(
                because: $"'{shell.ExecutablePath}' for '{shell.DisplayName}' must exist on disk");
        }
    }

    [Fact]
    public async Task GetAvailableShells_CachesResultAsync()
    {
        // Arrange
        ShellDetectionService service = new();

        // Act
        IReadOnlyList<ShellInfo> first = await service.GetAvailableShellsAsync();
        IReadOnlyList<ShellInfo> second = await service.GetAvailableShellsAsync();

        // Assert
        first.Should().BeSameAs(second);
    }

    [Fact]
    public async Task GetAvailableShells_NoDuplicateDisplayNamesAsync()
    {
        // Arrange
        ShellDetectionService service = new();

        // Act
        IReadOnlyList<ShellInfo> shells = await service.GetAvailableShellsAsync();

        // Assert
        shells.Select(s => s.DisplayName).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetAvailableShells_CommandPrompt_BuildsArgumentsAsync()
    {
        // Arrange
        ShellDetectionService service = new();
        ShellInfo cmd = (await service.GetAvailableShellsAsync()).First(s => s.DisplayName == "Command Prompt");

        // Act
        string args = cmd.BuildArguments("echo hello");

        // Assert
        args.Should().Be("/c echo hello");
    }

    [Fact]
    public async Task GetAvailableShells_CommandPrompt_BuildsElevatedArgumentsAsync()
    {
        // Arrange
        ShellDetectionService service = new();
        ShellInfo cmd = (await service.GetAvailableShellsAsync()).First(s => s.DisplayName == "Command Prompt");

        // Act
        string args = cmd.BuildElevatedArguments("echo hello", @"C:\temp\out.log");

        // Assert
        args.Should().Be("/c \"echo hello > \"C:\\temp\\out.log\" 2>&1\"");
    }

    [Fact]
    public async Task GetAvailableShells_CommandPrompt_EscapesDoubleQuotesAsync()
    {
        // Arrange
        ShellDetectionService service = new();
        ShellInfo cmd = (await service.GetAvailableShellsAsync()).First(s => s.DisplayName == "Command Prompt");

        // Act
        string escaped = cmd.EscapeCommand("echo \"hello\"");

        // Assert
        escaped.Should().Be("echo \\\"hello\\\"");
    }

    [Fact]
    public async Task GetAvailableShells_PowerShell_BuildsArgumentsAsync()
    {
        // Arrange
        ShellDetectionService service = new();
        ShellInfo? ps = (await service.GetAvailableShellsAsync())
            .FirstOrDefault(s => s.DisplayName is "PowerShell 7" or "Windows PowerShell");

        if (ps is null)
        {
            // Skip on machines without PowerShell (unlikely but safe).
            return;
        }

        // Act
        string args = ps.BuildArguments("Get-Process");

        // Assert
        args.Should().Be("-Command Get-Process");
    }

    [Fact]
    public async Task GetAvailableShells_PowerShell_BuildsElevatedArgumentsAsync()
    {
        // Arrange
        ShellDetectionService service = new();
        ShellInfo? ps = (await service.GetAvailableShellsAsync())
            .FirstOrDefault(s => s.DisplayName is "PowerShell 7" or "Windows PowerShell");

        if (ps is null)
        {
            return;
        }

        // Act
        string args = ps.BuildElevatedArguments("Get-Process", @"C:\temp\out.log");

        // Assert
        args.Should().Be("-Command \"& { Get-Process } *> 'C:\\temp\\out.log'\"");
    }

    [Fact]
    public async Task GetAvailableShells_WslShells_HaveWslPrefixAsync()
    {
        // Arrange
        ShellDetectionService service = new();

        // Act
        IReadOnlyList<ShellInfo> shells = await service.GetAvailableShellsAsync();
        IEnumerable<ShellInfo> wslShells = shells.Where(s => s.IsWsl);

        // Assert – all WSL shells should have "WSL · " prefix.
        foreach (ShellInfo shell in wslShells)
        {
            shell.DisplayName.Should().StartWith("WSL · ");
            shell.WslDistroName.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetAvailableShells_WslShells_BuildArgumentsAsync()
    {
        // Arrange
        ShellDetectionService service = new();
        ShellInfo? wsl = (await service.GetAvailableShellsAsync()).FirstOrDefault(s => s.IsWsl);

        if (wsl is null)
        {
            // Skip on machines without WSL installed.
            return;
        }

        // Act
        string args = wsl.BuildArguments("'ls -la'");

        // Assert – should use -d <distro> -- bash -c '<command>'.
        args.Should().Contain($"-d {wsl.WslDistroName}");
        args.Should().Contain("-- bash -c");
    }

    [Fact]
    public async Task GetAvailableShells_WslShells_EscapesSingleQuotesAsync()
    {
        // Arrange
        ShellDetectionService service = new();
        ShellInfo? wsl = (await service.GetAvailableShellsAsync()).FirstOrDefault(s => s.IsWsl);

        if (wsl is null)
        {
            return;
        }

        // Act
        string escaped = wsl.EscapeCommand("echo 'hello'");

        // Assert – single quotes are escaped using the '\'' pattern.
        escaped.Should().Be("'echo '\\''hello'\\'''");
    }

    [Fact]
    public async Task GetAvailableShells_NonWslShells_IsWslIsFalseAsync()
    {
        // Arrange
        ShellDetectionService service = new();

        // Act
        IReadOnlyList<ShellInfo> shells = await service.GetAvailableShellsAsync();
        IEnumerable<ShellInfo> nonWslShells = shells.Where(s => !s.IsWsl);

        // Assert
        foreach (ShellInfo shell in nonWslShells)
        {
            shell.WslDistroName.Should().BeNull();
        }
    }
}
